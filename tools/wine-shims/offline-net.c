/* Simulate "no internet" for one process tree (loaded via LD_PRELOAD).
 * connect() to anything that is not loopback fails with ENETUNREACH, so the
 * game can still reach the local EpinelPS server but nothing external.
 * The launcher itself is exempted (its process name is matched), since it is
 * only a UI/parent and legitimately talks to the local server. */
#define _GNU_SOURCE
#include <arpa/inet.h>
#include <dlfcn.h>
#include <errno.h>
#include <fcntl.h>
#include <netinet/in.h>
#include <string.h>
#include <sys/socket.h>
#include <unistd.h>

static int exempt(void)
{
    static int cached = -1;
    char comm[64] = {0};
    int fd, n;
    if (cached >= 0) return cached;
    fd = open("/proc/self/comm", O_RDONLY);
    if (fd >= 0) {
        n = read(fd, comm, sizeof(comm) - 1);
        close(fd);
        if (n > 0) comm[n] = '\0';
    }
    cached = (!strncmp(comm, "EpinelPSLaunch", 15) || !strncmp(comm, "dotnet", 6));
    return cached;
}

static int loopback(const struct sockaddr *addr)
{
    if (!addr) return 1;
    if (addr->sa_family == AF_INET) {
        const struct sockaddr_in *in = (const struct sockaddr_in *)addr;
        return (ntohl(in->sin_addr.s_addr) >> 24) == 127;
    }
    if (addr->sa_family == AF_INET6) {
        const struct sockaddr_in6 *in6 = (const struct sockaddr_in6 *)addr;
        if (IN6_IS_ADDR_LOOPBACK(&in6->sin6_addr)) return 1;
        if (IN6_IS_ADDR_V4MAPPED(&in6->sin6_addr))
            return in6->sin6_addr.s6_addr[12] == 127;
        return 0;
    }
    return 1; /* AF_UNIX and friends are local by definition */
}

int connect(int fd, const struct sockaddr *addr, socklen_t len)
{
    static int (*real)(int, const struct sockaddr *, socklen_t);
    if (!real) real = dlsym(RTLD_NEXT, "connect");
    if (!loopback(addr) && !exempt()) {
        errno = ENETUNREACH;
        return -1;
    }
    return real(fd, addr, len);
}
