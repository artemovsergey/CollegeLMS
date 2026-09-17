import json
import re

with open('import/wp_data_full.json', 'r') as f:
    wp = json.load(f)

pages_by_slug = {}
for p in wp['pages']:
    pages_by_slug[p['slug']] = p

def clean_html(html: str) -> str:
    if not html:
        return ""

    # Remove script blocks
    html = re.sub(r'<script[\s\S]*?</script>', '', html)
    # Remove style blocks
    html = re.sub(r'<style[\s\S]*?</style>', '', html)
    # Remove noscript
    html = re.sub(r'<noscript[\s\S]*?</noscript>', '', html)
    # Remove conditional comments
    html = re.sub(r'<!--\[if[\s\S]*?<!\[endif\]-->', '', html)
    html = re.sub(r'<!--\[if[\s\S]*?\[endif\]-->', '', html)
    # Remove nf-form blocks (Ninja Forms)
    html = re.sub(r'<noscript[\s\S]*?</noscript>', '', html)
    html = re.sub(r'<div[^>]*id="nf-form[^>]*>[\s\S]*?</div>\s*</div>', '', html)
    # Remove WordPress shortcodes like [gallery ...], [video ...], [audio ...]
    html = re.sub(r'\[/?[a-z_]+\s*[^\]]*\]', '', html)
    # Remove style attributes
    html = re.sub(r'\sstyle="[^"]*"', '', html)
    html = re.sub(r"\sstyle='[^']*'", '', html)
    # Remove class attributes
    html = re.sub(r'\sclass="[^"]*"', '', html)
    # Remove id attributes (keep only in anchors)
    html = re.sub(r'\sid="[^"]*"', '', html)
    # Remove clear br
    html = re.sub(r'<br\s+style="clear:\s*both"\s*/?>', '', html)
    # Replace http://stvcc.ru with empty (relative links)
    html = html.replace('http://stvcc.ru', '')
    # Replace HTML entities
    html = html.replace('&#171;', '\u00ab')
    html = html.replace('&#187;', '\u00bb')
    html = html.replace('&#8212;', '\u2014')
    html = html.replace('&#8211;', '\u2013')
    html = html.replace('&#8243;', '\u2033')
    html = html.replace('&#8242;', '\u2032')
    html = html.replace('&#8230;', '\u2026')
    html = html.replace('&nbsp;', ' ')
    html = html.replace('&#038;', '&')
    html = html.replace('&#8217;', "'")
    html = html.replace('&#8220;', '\u201c')
    html = html.replace('&#8221;', '\u201d')
    # Remove multiple empty p tags
    html = re.sub(r'<p>\s*</p>', '', html)
    html = re.sub(r'<p[^>]*>\s*</p>', '', html)
    # Remove multiple spaces
    html = re.sub(r'  +', ' ', html)
    # Remove empty lines
    html = re.sub(r'\n\s*\n', '\n', html)
    return html.strip()

target_slugs = {
    'distancionnoeobuch': 'Дистанционное обучение',
    'svidetelstvo-ob-akkreditatsii': 'Свидетельство об аккредитации',
    'tsentr-sodejstviya-trudoustrojstvu-vypusknikov': 'Центр содействия трудоустройству выпускников',
    'trudoustroystvo-i-karera': 'Электронная газета',
    'aktualnyie-vakansii': 'Актуальные вакансии',
    'ostavit-rezyume-dlya-poiska-rabotyi': 'Оставить резюме',
    'poleznyie-ssyilki': 'Полезные ссылки',
}

# Aggregate trudoustroystvo page content
trudo_slugs = [
    'tsentr-sodejstviya-trudoustrojstvu-vypusknikov',
    'trudoustroystvo-i-karera',
    'aktualnyie-vakansii',
    'ostavit-rezyume-dlya-poiska-rabotyi',
    'poleznyie-ssyilki',
]

def aggregate_trudoustroystvo():
    sections = []
    for slug in trudo_slugs:
        page = pages_by_slug.get(slug)
        if not page:
            print(f"  WARNING: {slug} not found in WP data")
            continue
        title = page['title']['rendered']
        content = clean_html(page['content']['rendered'])
        sections.append(f'<h2>{title}</h2>\n{content}')
    
    result = '\n\n'.join(sections)
    return result

print("Extracting content...\n")

# Extract trudoustroystvo
trudo_content = aggregate_trudoustroystvo()
print(f"trudoustroystvo: {len(trudo_content)} chars")

# Extract individual pages
for slug, title in target_slugs.items():
    page = pages_by_slug.get(slug)
    if page:
        cleaned = clean_html(page['content']['rendered'])
        print(f"{slug}: {len(cleaned)} chars (original: {len(page['content']['rendered'])})")
    else:
        print(f"{slug}: NOT FOUND in WP data")

print("\n=== OUTPUT JSON ===\n")
def get_rendered(slug):
    page = pages_by_slug.get(slug)
    if not page:
        return ""
    c = page.get('content', {})
    if isinstance(c, dict):
        return c.get('rendered', '')
    return str(c)

output = {
    "distancionnoeobuch": {
        "title": "Дистанционное обучение",
        "content": clean_html(get_rendered('distancionnoeobuch'))
    },
    "svidetelstvo-ob-akkreditatsii": {
        "title": "Свидетельство об аккредитации",
        "content": clean_html(get_rendered('svidetelstvo-ob-akkreditatsii'))
    },
    "trudoustroystvo": {
        "title": "Трудоустройство",
        "content": trudo_content
    }
}

print(json.dumps(output, ensure_ascii=False, indent=2))
