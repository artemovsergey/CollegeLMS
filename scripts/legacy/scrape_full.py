import requests
import json
from urllib.parse import urljoin
import time
import sys

BASE_URL = "http://stvcc.ru"
API_BASE = urljoin(BASE_URL, "/wp-json/wp/v2/")


def log(msg):
    print(msg)
    sys.stdout.flush()


def fetch_all(endpoint, per_page=100, use_embed=True):
    items = []
    page = 1
    while True:
        url = f"{endpoint}?per_page={per_page}&page={page}"
        if use_embed:
            url += "&_embed"
        resp = requests.get(url, timeout=30)
        if resp.status_code != 200:
            log(f"  HTTP {resp.status_code} for {url}")
            break
        data = resp.json()
        if not data:
            break
        items.extend(data)
        total_pages = int(resp.headers.get("X-WP-TotalPages", 1))
        log(f"  Page {page}/{total_pages} ({len(data)} items, total: {len(items)})")
        if page >= total_pages:
            break
        page += 1
        time.sleep(0.3)
    return items


if __name__ == "__main__":
    log("[check] REST API...")
    root_resp = requests.get(API_BASE, timeout=10)
    if root_resp.status_code != 200:
        log("[err] REST API unavailable")
        exit()

    all_data = {}

    log("[categories] fetching...")
    all_data["categories"] = fetch_all(f"{API_BASE}categories", use_embed=False)
    for c in all_data["categories"]:
        name = c.get("name", c.get("slug", "?"))
        log(f"  -> {c['id']}: {name} ({c.get('count', 0)} posts)")

    log("[posts] fetching (3720)...")
    all_data["posts"] = fetch_all(f"{API_BASE}posts", use_embed=True)

    log("[pages] fetching (211)...")
    all_data["pages"] = fetch_all(f"{API_BASE}pages", use_embed=True)

    output_path = "import/wp_data_full.json"
    with open(output_path, "w", encoding="utf-8") as f:
        json.dump(all_data, f, ensure_ascii=False, indent=2)
    log(f"[done] saved to {output_path}")
    log(f"stats: {len(all_data['categories'])} cats, {len(all_data['posts'])} posts, {len(all_data['pages'])} pages")
