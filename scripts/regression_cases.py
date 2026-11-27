"""Regression cases for scripts/regression.py (see that file for usage).

Each case is a function taking the run Context and returning an optional short
note. Use ctx.fresh() for a brand-new account (exact assertions) and
ctx.admin_client() only for checks that need a progressed save. Raise via
check()/eq() on mismatch. Add a case for every server fix you make.

IDs used here are documented in EPINELPS-FIXES.md / CONTEXT.md.
"""
import json
import re

from regression import CaseFailure, check, eq

RED_ASH_EVENT = 40031          # archive Red Ash story dungeon event id
RED_ASH_1_3 = 103110103        # non-repeat stage, ClearRewardId -> BGM 8995003
CHALLENGE_EVENT = 60123        # Little Witch Trick challenge
CHALLENGE_STAGE = 212310101
NKS_ARCHIVE_EVENT = 80803      # IsArchiveEvent row, content manager 2
ARCHIVE_DICE = 4               # Blue Water Island / Volt Road board 1004
DICE_ITEM = 7061002
ISLAND_ARCHIVE_MGR = 2         # Beauty Full Shot archive manager


def _items(client) -> dict:
    inv = client.call("/inventory/get")
    out = {}
    for it in inv.get("Items", []):
        out[it.get("Tid", 0)] = out.get(it.get("Tid", 0), 0) + it.get("Count", 0)
    return out


def _item_isn(client, tid: int) -> int:
    for it in client.call("/inventory/get").get("Items", []):
        if it.get("Tid") == tid:
            return it["Isn"]
    raise CaseFailure(f"item {tid} not in inventory")


# ---------------------------------------------------------------- events

def challenge_tickets(ctx):
    """Challenge event: lazy 3-ticket grant, first clear / repeat rewards, 3->2->1"""
    c = ctx.fresh()
    req = {"EventId": CHALLENGE_EVENT}
    eq(c.call("/event/challengestage/get", req).get("RemainTicket", 0), 3, "initial tickets")
    stage = {"EventId": CHALLENGE_EVENT, "StageId": CHALLENGE_STAGE, "TeamNumber": 1}
    c.call("/event/challengestage/enter", stage)
    r1 = c.call("/event/challengestage/clear", {**stage, "BattleResult": 1})
    eq(r1.get("RemainTicket", 0), 2, "tickets after first clear")
    check(r1.get("FirstClearReward", {}).get("Currency"), "first clear pays FirstClearReward")
    check("Reward" in r1, "Reward must be non-null")
    r2 = c.call("/event/challengestage/clear", {**stage, "BattleResult": 1})
    eq(r2.get("RemainTicket", 0), 1, "tickets after repeat")
    check(r2.get("Reward", {}).get("Currency"), "repeat pays Reward")
    check(not r2.get("FirstClearReward"), "repeat must not pay FirstClearReward again")
    g = c.call("/event/challengestage/get", req)
    eq(g.get("RemainTicket", 0), 1, "tickets persisted")
    cleared = [s.get("StageId") for s in g.get("LastClearedEventStageList", [])]
    check(CHALLENGE_STAGE in cleared, f"stage recorded as cleared: {cleared}")


def event_shop_fallback(ctx):
    """Archived events resolve their original shop (9000000 + id%1000*100 + 1)"""
    c = ctx.fresh()
    notes = []
    for ev, shop_id in ((40031, 9003101), (40011, 9001101), (40123, 9012301)):
        shops = c.call("/event/shopproductlist", {"EventId": ev}).get("Shops", [])
        tids = [s.get("ShopTid") for s in shops]
        check(shop_id in tids, f"event {ev}: expected shop {shop_id}, got {tids}")
        n = len(next(s for s in shops if s.get("ShopTid") == shop_id).get("List", []))
        check(n >= 5, f"event {ev}: shop {shop_id} has only {n} products")
        notes.append(f"{ev}:{n}")
    return " ".join(notes)


def event_shop_buy(ctx):
    """Event shop purchase deducts the price item, grants goods, persists buy count"""
    c = ctx.fresh()
    price_item, order, product = 7060031, 5, 9003105  # Red Ash shop, priced 400 x item
    ctx.admin.cmd("add-item", c.uid, price_item, 1000)
    r = c.call("/event/shopbuyproduct", {"ShopCategory": 7, "Order": order,
                                          "ShopProductTid": product, "Quantity": 1,
                                          "EventId": RED_ASH_EVENT})
    check(r.get("Product"), f"purchase failed (empty response): {r}")
    eq((r.get("Item", {}).get("Tid"), r.get("Item", {}).get("Count", 0)), (price_item, 600),
       "price item remaining in response")
    eq(r["Product"].get("BuyCount", 0), 1, "response BuyCount")
    check(r["Product"].get("Currency") or r["Product"].get("Item"), "goods granted")
    eq(_items(c).get(price_item), 600, "price item persisted")
    shop = c.call("/event/shopproductlist", {"EventId": RED_ASH_EVENT})["Shops"][0]
    bought = [p.get("BuyCount", 0) for p in shop["List"] if p.get("Order") == order]
    eq(bought, [1], "buy count persisted")


def anomalous_rewards_nonnull(ctx):
    """Anomalous intercept FastClear/Finish send all four reward messages"""
    c = ctx.fresh()
    for path, req in (("/intercept/Anomalous/FastClear", {"InterceptAnomalousId": 1}),
                      ("/intercept/Anomalous/Finish", {"InterceptAnomalousId": 1, "DamageDealt": 1000})):
        r = c.call(path, req)
        for f in ("NormalReward", "BonusReward", "NormalRewardByRewardUpEvent", "BonusRewardByRewardUpEvent"):
            check(f in r, f"{path}: {f} is null")
        eq(r.get("MaxTicketCount", 0), 5, f"{path} MaxTicketCount")


# ---------------------------------------------------------------- archive

def archive_first_clear_reward(ctx):
    """Archive Red Ash 1-3: first clear pays ClearRewardId (BGM 8995003), repeat pays nothing"""
    c = ctx.fresh()
    c.call("/archive/storydungeon/get", {"EventId": RED_ASH_EVENT})
    stage = {"EventId": RED_ASH_EVENT, "StageId": RED_ASH_1_3, "TeamNumber": 1}
    c.call("/archive/storydungeon/enterstage", stage)
    r1 = c.call("/archive/storydungeon/clearstage", {**stage, "BattleResult": 1})
    eq(r1.get("Reward", {}).get("JukeboxBgm"), [8995003], "first-clear BGM")
    r2 = c.call("/archive/storydungeon/clearstage", {**stage, "BattleResult": 1})
    check("Reward" in r2, "repeat Reward must be non-null")
    check(not r2["Reward"].get("JukeboxBgm"), "non-repeat stage paid again on repeat")


def archive_event_quests(ctx):
    """Archive event quests: 6 mappings, cleared-only Current/Cumulative lists, acquire once"""
    c = ctx.fresh()
    aq = c.call("/archive/get")["ArchiveEventQuest"]
    maps = aq.get("EventQuestMappingList", [])
    eq(len(maps), 6, "episode mappings")
    for m in maps:
        # Uncleared ids here crash the lobby badge scan (TableId=0).
        check(not m.get("CurrentArchiveEventQuestIdList"), "fresh Current list must be empty")
        check(not m.get("CumulativeArchiveEventQuestIdList"), "fresh Cumulative list must be empty")
    m0 = maps[0]
    mid = m0["ArchiveRecordManagerId"]
    eq(mid, 1000100, "first episode")
    quest = m0["EventQuestIdList"][0]
    stage = m0["EventQuestStageList"][0]["StageId"]
    c.call("/archive/event-quest/activate", {"ArchiveRecordManagerId": mid})
    c.call("/archive/event-quest/clear", {"ArchiveRecordManagerId": mid, "ArchiveEventQuestId": quest})
    c.call("/archive/event-quest/stage/enter", {"ArchiveRecordManagerId": mid, "EventQuestStageId": stage})
    c.call("/archive/event-quest/stage/clear", {"ArchiveRecordManagerId": mid,
                                                "EventQuestStageId": stage, "BattleResult": 1})
    a1 = c.call("/archive/event-quest/acquire", {"ArchiveRecordManagerId": mid})
    check(a1.get("Reward", {}).get("Item"), "acquire pays quest/stage rewards")
    a2 = c.call("/archive/event-quest/acquire", {"ArchiveRecordManagerId": mid})
    eq(a2["_raw"], "0a00", "second acquire = empty non-null Reward")
    aq = c.call("/archive/get")["ArchiveEventQuest"]
    eq(aq.get("ActivatedArchiveRecordManagerId"), mid, "activated episode")
    m = next(x for x in aq["EventQuestMappingList"] if x["ArchiveRecordManagerId"] == mid)
    eq(m.get("CurrentArchiveEventQuestIdList"), [quest], "Current = cleared quests")
    eq(m.get("CumulativeArchiveEventQuestIdList"), [quest], "Cumulative = cleared quests")
    c.call("/archive/event-quest/reset", {"ArchiveRecordManagerId": mid})
    a3 = c.call("/archive/event-quest/acquire", {"ArchiveRecordManagerId": mid})
    eq(a3["_raw"], "0a00", "reset must not allow re-claiming")


def archive_nks_manager2(ctx):
    """Red Ash NKS: getdata and nksv2/get (nKsId=3) agree on 28 manager-2 missions"""
    c = ctx.fresh()
    doc = json.loads(c.call("/archive/minigame/getdata", {"EventId": NKS_ARCHIVE_EVENT})["Json"])
    for k in ("NksArchiveMissionSerializable", "NksMiniOutGameStateSerializable"):
        check(k in doc, f"getdata missing {k}")
    blob = json.dumps(doc["NksArchiveMissionSerializable"])
    doc_nums = {int(x) for x in re.findall(r"\b\d{7}\b", blob)}
    n = c.call("/arcade/nksv2/get", {"NKsId": 3})
    ids = sorted(m["NKsMissionId"] for m in n.get("MissionProgressList", []))
    eq(len(ids), 28, "manager-2 mission count")
    # Manager 2 ids are 1101001..1306000; the live v2 manager 3 (39 missions)
    # uses 2xxxxxx, which is what the client's nKsId=3 would wrongly build.
    wrong = [i for i in ids if not 1100000 < i < 2000000]
    check(not wrong, f"non manager-2 mission ids: {wrong}")
    missing = [i for i in ids if i not in doc_nums]
    check(not missing, f"nksv2/get missions absent from getdata: {missing}")
    # setdata round-trip
    c.call("/archive/minigame/setdata", {"EventId": NKS_ARCHIVE_EVENT, "Json": json.dumps(doc)})
    again = json.loads(c.call("/archive/minigame/getdata", {"EventId": NKS_ARCHIVE_EVENT})["Json"])
    eq(sorted(again), sorted(doc), "setdata round-trip keys")


def archive_dice(ctx):
    """Archive dice: 20 dice granted, 1 consumed per roll, board ids in 301..330 path"""
    c = ctx.fresh()
    g = c.call("/archive/minigame/dice/get", {"DiceMiniGameId": ARCHIVE_DICE})
    eq(g.get("CurrentBoardId", 0), 1, "start sentinel")
    eq(_items(c).get(DICE_ITEM), 20, "starting dice")
    pos = []
    for _ in range(3):
        r = c.call("/archive/minigame/dice/roll", {"DiceMiniGameId": ARCHIVE_DICE})
        check(1 <= r.get("DiceResult", 0) <= 6, f"dice result {r.get('DiceResult')}")
        check(301 <= r.get("CurrentBoardId", 0) <= 330, f"board id {r.get('CurrentBoardId')}")
        check("EventReward" in r, "EventReward must be non-null")
        pos.append(r["CurrentBoardId"])
    eq(_items(c).get(DICE_ITEM), 17, "dice after 3 rolls")
    eq(c.call("/archive/minigame/dice/get", {"DiceMiniGameId": ARCHIVE_DICE}).get("CurrentBoardId"),
       pos[-1], "position persisted")
    return "path " + "->".join(map(str, pos))


def archive_island_adventure(ctx):
    """Island adventure: starter kit, every spot listed, failed cast keeps spot fish"""
    c = ctx.fresh()
    mgr = {"IslandAdventureManagerId": ISLAND_ARCHIVE_MGR}
    d = c.call("/archive/minigame/islandadventure/getalldata", mgr)
    inv = d["InventoryData"]
    eq(inv.get("EquipFishingRodId"), 101, "starter rod")
    eq(inv.get("EquipFishingBaitId"), 201, "starter bait")
    eq(inv.get("ListItemId", {}).get(201), 30, "starter bait count")
    spots = [s["SpotId"] for s in d["FishingSpotCountData"]["FishingSpotCountHistory"]]
    check(len(spots) >= 30 and len(set(spots)) == len(spots), f"spot list incomplete: {len(spots)}")
    spot = spots[0]
    r = c.call("/archive/minigame/islandadventure/fish", {**mgr, "FishingSpotId": spot})
    eq(r.get("RemainingBaitCount", 0), 29, "bait used by failed cast")
    eq(r.get("Currency", 0), 0, "failed cast pays nothing")
    hist = c.call("/archive/minigame/islandadventure/get/fish/spotcount", mgr)["FishingSpotCountHistory"]
    after = {h["SpotId"]: h.get("CatchCount", 0) for h in hist}
    eq(sorted(after), sorted(spots), "spotcount lists the same spots as getalldata")
    eq(after[spot], 0, "failed cast must not consume a spot fish")


def archive_minigame_typed(ctx):
    """Every /archive/minigame/* route answers 200 and decodes as its own type"""
    c = ctx.fresh()
    ids = {"IslandAdventureManagerId": ISLAND_ARCHIVE_MGR, "DiceMiniGameId": ARCHIVE_DICE,
           "EventId": NKS_ARCHIVE_EVENT}
    routes = sorted(p for p in ctx.schema.routes if p.startswith("/archive/minigame/"))
    check(len(routes) >= 40, f"only {len(routes)} archive minigame routes registered")
    bad = []
    for p in routes:
        rt = ctx.schema.routes[p]
        fields = ctx.schema.messages.get(rt["req"] or "", {})
        req = {k: v for k, v in ids.items() if k in fields}
        try:
            r = c.call(p, req)
            if "_unknown" in r:
                bad.append(f"{p}: fields not in {rt['res']}")
        except CaseFailure as e:
            bad.append(str(e)[:120])
    check(not bad, "; ".join(bad))
    return f"{len(routes)} routes"


# ---------------------------------------------------------------- characters

def sr_limit_break(ctx):
    """SR limit break consumes spare bodies, sets Grade, stops at max"""
    c = ctx.fresh()
    rapi, piece = 201001, 5100010
    ctx.admin.cmd("add-character", c.uid, rapi)
    ctx.admin.cmd("add-item", c.uid, piece, 3)
    chars = c.call("/character/get").get("Character", [])
    ch = next((x["Default"] for x in chars if x["Default"].get("Tid", 0) // 100 == rapi // 100), None)
    check(ch, "added character not found")
    isn = _item_isn(c, piece)
    r = c.call("/character/upgrade", {"Csn": ch["Csn"], "Isn": isn, "Count": 2})
    eq(r.get("Character", {}).get("Grade", 0), 2, "grade after 2 bodies")
    eq(_items(c).get(piece, 0), 1, "bodies left")
    r = c.call("/character/upgrade", {"Csn": ch["Csn"], "Isn": isn, "Count": 1})
    eq(_items(c).get(piece, 0), 1, "upgrade past SR max must not consume the body")


CASES = [
    challenge_tickets,
    event_shop_fallback,
    event_shop_buy,
    anomalous_rewards_nonnull,
    archive_first_clear_reward,
    archive_event_quests,
    archive_nks_manager2,
    archive_dice,
    archive_island_adventure,
    archive_minigame_typed,
    sr_limit_break,
]
