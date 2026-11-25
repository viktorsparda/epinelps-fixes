/* Redirect cloud.nikke-kr.com to the local EpinelPS server for one process
 * tree (loaded via LD_PRELOAD).  Used so cutscene videos are served locally
 * over TLS with the server's own certificate; the prefix trusts the CA, so
 * wininet's chain and hostname checks both pass.  No /etc/hosts change. */
#define _GNU_SOURCE
#include <dlfcn.h>
#include <netdb.h>
#include <string.h>
#include <netinet/in.h>

static const char *const TARGET = "cloud.nikke-kr.com";
static const char *const LOOPBACK = "127.0.0.1";

typedef int (*getaddrinfo_fn)(const char *, const char *, const struct addrinfo *, struct addrinfo **);

int getaddrinfo(const char *node, const char *service,
                const struct addrinfo *hints, struct addrinfo **res)
{
    static getaddrinfo_fn real;
    if (!real)
        real = (getaddrinfo_fn)dlsym(RTLD_NEXT, "getaddrinfo");
    if (node && strcmp(node, TARGET) == 0)
        node = LOOPBACK;
    return real(node, service, hints, res);
}

struct hostent *gethostbyname(const char *name)
{
    static struct hostent *(*real)(const char *);
    if (!real)
        real = dlsym(RTLD_NEXT, "gethostbyname");
    if (name && strcmp(name, TARGET) == 0)
        name = LOOPBACK;
    return real(name);
}
