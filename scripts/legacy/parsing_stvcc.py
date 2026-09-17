import requests
import json
from urllib.parse import urljoin

BASE_URL = "http://stvcc.ru"  # <-- ЗАМЕНИТЕ НА НУЖНЫЙ ДОМЕН
API_BASE = urljoin(BASE_URL, "/wp-json/wp/v2/")

def fetch_all_pages(endpoint, per_page=100):
    """Забирает все объекты с пагинацией, учитывая заголовок X-WP-TotalPages"""
    items = []
    page = 1
    while True:
        url = f"{endpoint}?per_page={per_page}&page={page}&_fields=id,slug,title,link,parent,type,taxonomies"
        resp = requests.get(url)
        if resp.status_code != 200:
            break
        data = resp.json()
        if not data:
            break
        items.extend(data)
        total_pages = int(resp.headers.get("X-WP-TotalPages", 1))
        if page >= total_pages:
            break
        page += 1
        print(f"  Страница API: {page}/{total_pages} для {endpoint}")
    return items

def build_tree(pages):
    """Строит иерархию родитель/дочерний для страниц"""
    lookup = {p['id']: {**p, 'children': []} for p in pages}
    roots = []
    for p_id, node in lookup.items():
        parent_id = node.get('parent', 0)
        if parent_id and parent_id in lookup:
            lookup[parent_id]['children'].append(node)
        else:
            roots.append(node)
    return roots

def print_tree(nodes, indent=0):
    """Рекурсивная печать дерева"""
    for node in sorted(nodes, key=lambda x: x.get('title', {}).get('rendered', '')):
        title = node.get('title', {}).get('rendered', 'Без названия')
        slug = node.get('slug', '')
        print('  ' * indent + f"├─ {title} (/{slug})")
        if node.get('children'):
            print_tree(node['children'], indent + 1)

if __name__ == "__main__":
    print("🔍 Определяю доступные типы записей...")
    
    # 1. Смотрим корневой эндпоинт, чтобы найти все CPT
    root_resp = requests.get(API_BASE)
    if root_resp.status_code != 200:
        print("❌ REST API недоступен")
        exit()
        
    available_routes = root_resp.json().get('routes', {})
    post_types = set()
    
    # Ищем все эндпоинты вида /wp/v2/<type>
    for route in available_routes:
        if route.startswith('/wp/v2/') and not route.endswith('/') and '?' not in route:
            pt = route.replace('/wp/v2/', '')
            if pt not in ['posts', 'pages', 'categories', 'tags', 'media', 'blocks', 'templates', 'users', 'comments', 'settings', 'themes', 'plugins', 'search']:
                post_types.add(pt)
    
    print(f"📋 Стандартные типы: posts, pages")
    if post_types:
        print(f"📋 Найденные CPT: {', '.join(post_types)}")
    
    all_data = {}
    
    # 2. Собираем страницы (иерархия)
    print("\n📄 Собираю страницы...")
    pages = fetch_all_pages(f"{API_BASE}pages")
    all_data['pages'] = pages
    
    # 3. Собираем посты (плоские)
    print("\n📝 Собираю посты...")
    posts = fetch_all_pages(f"{API_BASE}posts")
    all_data['posts'] = posts
    
    # 4. Собираем рубрики
    print("\n📁 Собираю рубрики...")
    categories = fetch_all_pages(f"{API_BASE}categories")
    all_data['categories'] = categories
    
    # 5. Собираем пользовательские типы
    for pt in sorted(post_types):
        print(f"\n📦 Собираю {pt}...")
        cpt_items = fetch_all_pages(f"{API_BASE}{pt}")
        if cpt_items:
            all_data[pt] = cpt_items
    
    # 6. Сохраняем сырые данные в JSON
    with open('wp_structure_raw.json', 'w', encoding='utf-8') as f:
        json.dump(all_data, f, ensure_ascii=False, indent=2)
    print("\n💾 Сырые данные сохранены в wp_structure_raw.json")
    
    # 7. Выводим красивое дерево страниц
    print("\n" + "="*60)
    print("🌳 ДЕРЕВО СТРАНИЦ:")
    print("="*60)
    tree = build_tree(pages)
    print_tree(tree)
    
    # 8. Выводим статистику
    print("\n" + "="*60)
    print("📊 СТАТИСТИКА:")
    print(f"  Страниц: {len(pages)}")
    print(f"  Постов: {len(posts)}")
    print(f"  Рубрик: {len(categories)}")
    for pt in sorted(post_types):
        if pt in all_data:
            print(f"  {pt}: {len(all_data[pt])}")