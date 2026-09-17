import json
import uuid
import pg8000
import re
from datetime import datetime

DB_CONFIG = {
    "host": "localhost",
    "port": 5432,
    "database": "collegelms",
    "user": "postgres",
    "password": "root",
}

JSON_PATH = "import/wp_data_full.json"


def sanitize(s):
    replaces = {
        "&#8212;": "\u2014", "&#8211;": "\u2013", "&#8220;": "\u201c",
        "&#8221;": "\u201d", "&#8216;": "\u2018", "&#8217;": "\u2019",
        "&#8243;": "\u2033", "&hellip;": "\u2026", "&nbsp;": " ",
        "&amp;": "&", "&laquo;": "\u00ab", "&raquo;": "\u00bb",
        "&#039;": "'", "&lt;": "<", "&gt;": ">",
    }
    for k, v in replaces.items():
        s = s.replace(k, v)
    return s.strip()


def main():
    print("[load] reading JSON...")
    with open(JSON_PATH, "r", encoding="utf-8") as f:
        data = json.load(f)

    print(f"[db] connecting to {DB_CONFIG['host']}:{DB_CONFIG['port']}/{DB_CONFIG['database']}...")
    conn = pg8000.connect(**DB_CONFIG)
    conn.autocommit = False
    cur = conn.cursor()

    try:
        # Find admin user
        cur.execute("SELECT id FROM users WHERE role = 'Admin' ORDER BY created_at LIMIT 1")
        row = cur.fetchone()
        if row:
            admin_id = row[0]
            print(f"[ok] admin user: {admin_id}")
        else:
            admin_id = uuid.uuid4()
            print(f"[warn] no admin found, using placeholder: {admin_id}")

        # --- 1. Categories ---
        print("\n[categories] importing...")
        wp_cat_map = {}  # wp_id -> college_id

        for cat in data.get("categories", []):
            wp_id = cat["id"]
            name = cat.get("name", "")
            slug = cat.get("slug", "")
            if not name:
                continue

            cur.execute(
                "SELECT id FROM news_categories WHERE slug = %s", (slug,)
            )
            existing = cur.fetchone()
            if existing:
                wp_cat_map[wp_id] = existing[0]
                print(f"  skip (exists): {name} -> {existing[0]}")
                continue

            cat_id = uuid.uuid4()
            now = datetime.utcnow()
            cur.execute(
                "INSERT INTO news_categories (id, name, slug, created_at, updated_at) VALUES (%s, %s, %s, %s, %s)",
                (cat_id, name, slug, now, now),
            )
            wp_cat_map[wp_id] = cat_id
            print(f"  created: {name} ({slug}) -> {cat_id}")

        conn.commit()
        print(f"[done] {len(wp_cat_map)} categories")

        # --- 2. Posts ---
        print("\n[posts] importing...")
        imported = 0
        skipped = 0
        errors = 0

        for post in data.get("posts", []):
            try:
                slug = post.get("slug", "")
                title = sanitize(post.get("title", {}).get("rendered", ""))
                content_html = post.get("content", {}).get("rendered", "")
                date_str = post.get("date", "")
                status = post.get("status", "")

                if not title:
                    skipped += 1
                    continue

                # Check duplicate by slug
                if slug:
                    cur.execute("SELECT id FROM news WHERE slug = %s", (slug,))
                    if cur.fetchone():
                        skipped += 1
                        continue

                published_at = None
                if date_str:
                    try:
                        published_at = datetime.fromisoformat(date_str)
                    except:
                        published_at = datetime.utcnow()
                else:
                    published_at = datetime.utcnow()

                # Image
                image_url = None
                embedded = post.get("_embedded", {})
                media = embedded.get("wp:featuredmedia", [])
                if media:
                    image_url = media[0].get("source_url")

                # Category mapping
                category_id = None
                for cid in post.get("categories", []):
                    if cid in wp_cat_map:
                        category_id = wp_cat_map[cid]
                        break

                news_id = uuid.uuid4()
                now = datetime.utcnow()
                is_published = status == "publish"

                cur.execute(
                    """INSERT INTO news (id, title, slug, content, image_url, category_id,
                                         is_published, published_at, is_deleted, created_by_id, created_at, updated_at)
                       VALUES (%s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s)""",
                    (news_id, title, slug, content_html, image_url, category_id,
                     is_published, published_at, False, admin_id, now, now),
                )
                imported += 1

                if imported % 500 == 0:
                    conn.commit()
                    print(f"  ... {imported} posts imported")

            except Exception as e:
                errors += 1
                print(f"  [err] post {post.get('id', '?')}: {e}")
                conn.rollback()

        conn.commit()
        print(f"\n[done] import complete:")
        print(f"  imported: {imported}")
        print(f"  skipped:  {skipped}")
        print(f"  errors:   {errors}")

    finally:
        cur.close()
        conn.close()


if __name__ == "__main__":
    main()
