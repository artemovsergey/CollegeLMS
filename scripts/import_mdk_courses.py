#!/usr/bin/env python3
"""Импорт курсов МДК из GitHub-репозиториев в CollegeLMS.

Скрипт скачивает содержимое репозитория (разделы с лекциями, практическими и
самостоятельными работами), создаёт курс в LMS и наполняет его:

* занятия — из markdown-файлов репозитория (`Лекция` → Lecture,
  `Практическая работа` → Practice, `Самостоятельная работа` → SelfStudy);
* документы — КТП, методички, архивы студенческих проектов;
* материалы — презентации, картинки из лекций, ноутбуки и архивы МДК 02.03;
* группы — по названию (`ИП 245`, `ИБ 243`, ...).

Использование:

    export ADMIN_TOKEN=...            # JWT администратора CollegeLMS
    export LMS_API=http://127.0.0.1:5026
    export LMS_TEACHER_ID=...         # id преподавателя-владельца курсов
    python3 scripts/import_mdk_courses.py --cache /tmp/mdk --dry-run
    python3 scripts/import_mdk_courses.py --cache /tmp/mdk

Для переписывания ссылок на картинки нужен доступ к БД (только чтение) —
пути загруженных файлов API не отдаёт:

    export PGPASSWORD=... DB_HOST=127.0.0.1 DB_PORT=5432 DB_USER=... DB_NAME=...
"""

from __future__ import annotations

import argparse
import base64
import html
import json
import mimetypes
import os
import re
import subprocess
import sys
import time
import urllib.parse
import urllib.request
import uuid
from concurrent.futures import ThreadPoolExecutor
from dataclasses import dataclass, field
from pathlib import Path

OWNER = "artemovsergey"
BRANCH = "master"
RAW = "https://raw.githubusercontent.com/{owner}/{repo}/{branch}/{path}"
API = "https://api.github.com/repos/{owner}/{repo}/git/trees/{branch}?recursive=1"

IMAGE_EXTS = {".png", ".jpg", ".jpeg", ".gif", ".webp", ".svg"}

KIND_BY_PREFIX = [
    ("Практическая работа", "Practice"),
    ("Самостоятельная работа", "SelfStudy"),
    ("Контрольная работа", "SelfStudy"),
    ("Тест", "SelfStudy"),
    ("Лекция", "Lecture"),
]
KIND_ORDER = {"Lecture": 0, "Practice": 1, "SelfStudy": 2}

# Ограничение колонки lessons.content (varchar(65535)).
CONTENT_LIMIT = 65535

COURSES: list[dict] = [
    {
        "repo": "mdk0102",
        "title": "МДК 01.02 Поддержка и тестирование программных модулей",
        "description": (
            "Ручное и автоматизированное тестирование программных модулей на платформе .NET: "
            "тест-дизайн и анализ требований, xUnit и Moq, Selenium WebDriver, тестирование "
            "REST API (RestSharp), интеграционные тесты с EF Core и SQLite, нагрузочное "
            "тестирование (k6), логирование (Serilog) и непрерывная интеграция (GitHub Actions). "
            "Источник: github.com/artemovsergey/mdk0102."
        ),
        "groups": ["ИП 245", "ИП 246"],
        "documents": ["tutorial/*.pdf"],
        "exam_lessons": [("Экзамен.md", "Экзамен. Варианты заданий")],
    },
    {
        "repo": "mdk0103_IB",
        "title": "МДК 01.03 Разработка мобильных приложений (информационная безопасность)",
        "description": (
            "Разработка мобильных приложений на React Native (Expo) и TypeScript: настройка "
            "окружения, компоненты и состояние, навигация, Material Design, сетевые запросы, "
            "фоновые задачи и уведомления, нативные возможности и сборка. "
            "Источник: github.com/artemovsergey/mdk0103_IB."
        ),
        "groups": ["ИБ 243"],
        "documents": ["КТП*.docx", "tutorial/*.pdf", "examen.pdf", "repositories/*"],
    },
    {
        "repo": "mdk0103_IP",
        "title": "МДК 01.03 Разработка мобильных приложений (информационные системы и программирование)",
        "description": (
            "Разработка мобильных приложений на React Native (Expo) и TypeScript: настройка "
            "окружения, компоненты и состояние, навигация, Material Design, сетевые запросы, "
            "фоновые задачи и уведомления, нативные возможности и сборка. "
            "Источник: github.com/artemovsergey/mdk0103_IP."
        ),
        "groups": ["ИП 242", "ИП 245", "ИП 246"],
        "documents": ["КТП*.docx", "tutorial/*.pdf", "repositories/*"],
        "exam_lessons": [("examen.md", "Экзамен. Варианты заданий")],
    },
    {
        "repo": "mdk0104_IB",
        "title": "МДК 01.04 Системное программирование (информационная безопасность)",
        "description": (
            "Системное программирование на платформе .NET: язык C# и ООП, работа со строками и "
            "коллекциями, многопоточность и асинхронность, базы данных (Entity Framework Core + "
            "PostgreSQL), REST API на ASP.NET Core. "
            "Источник: github.com/artemovsergey/mdk0104_IB."
        ),
        "groups": ["ИБ 243"],
        "documents": ["КТП*.docx", "tutorial/*.pdf"],
        "exam_lessons": [("examen.md", "Экзамен. Материалы для подготовки")],
    },
    {
        "repo": "mdk0104_IP",
        "title": "МДК 01.04 Системное программирование (информационные системы и программирование)",
        "description": (
            "Системное программирование на платформе .NET: язык C# и ООП, работа со строками и "
            "коллекциями, многопоточность и асинхронность, обработка файлов, базы данных "
            "(Entity Framework Core + PostgreSQL), веб-приложения на ASP.NET Core, фоновые "
            "задачи (Hangfire), валидация (FluentValidation) и логирование (Serilog). "
            "Источник: github.com/artemovsergey/mdk0104_IP."
        ),
        "groups": ["ИП 242", "ИП 245", "ИП 246"],
        "documents": ["КТП*.docx", "tutorial/*.pdf"],
        "exam_lessons": [("examen.md", "Экзамен. Варианты заданий")],
    },
    {
        "repo": "mdk0901",
        "title": "МДК 09.01 Проектирование и разработка веб-приложений (React)",
        "description": (
            "Проектирование и разработка веб-приложений: HTML и семантическая вёрстка, CSS "
            "(Flexbox, Grid, адаптивность), JavaScript, TypeScript, React, .NET, интеграция "
            "фронтенда и бекенда, тестирование и развёртывание приложения. "
            "Источник: github.com/artemovsergey/mdk0901."
        ),
        "groups": ["ИВ 244"],
        "documents": ["КТП*.docx", "tutorial/*.pdf", "resources/*.pdf"],
    },
    {
        "repo": "mdk0203",
        "title": "МДК 02.03 Математическое моделирование",
        "description": (
            "Математическое моделирование на Python: основы языка и Jupyter Notebook, NumPy, "
            "Matplotlib, SciPy и Pandas. Занятия собраны из ноутбуков и архивов с заданиями. "
            "Источник: github.com/artemovsergey/mdk0203."
        ),
        "groups": ["ИП 232", "ИП 235", "ИП 236"],
        "layout": "notebooks",
    },
]


# --------------------------------------------------------------------------- #
# Загрузка с GitHub
# --------------------------------------------------------------------------- #
def github_json(url: str) -> dict:
    req = urllib.request.Request(url, headers={"User-Agent": "collegelms-import"})
    token = os.environ.get("GITHUB_TOKEN")
    if token:
        req.add_header("Authorization", f"Bearer {token}")
    for attempt in range(4):
        try:
            with urllib.request.urlopen(req, timeout=60) as resp:
                return json.loads(resp.read().decode("utf-8"))
        except Exception as err:  # noqa: BLE001
            if attempt == 3:
                raise
            time.sleep(2 * (attempt + 1))
    raise RuntimeError(url)


def repo_files(repo: str, cache: Path) -> list[str]:
    tree_file = cache / f"{repo}.tree.json"
    if tree_file.exists():
        return json.loads(tree_file.read_text(encoding="utf-8"))
    data = github_json(API.format(owner=OWNER, repo=repo, branch=BRANCH))
    files = [item["path"] for item in data.get("tree", []) if item["type"] == "blob"]
    cache.mkdir(parents=True, exist_ok=True)
    tree_file.write_text(json.dumps(files, ensure_ascii=False), encoding="utf-8")
    return files


CURL = ["curl", "-sfL", "--retry", "5", "--retry-all-errors", "--retry-delay", "2", "--max-time", "300"]


def download(repo: str, path: str, cache: Path) -> Path:
    dest = cache / repo / path
    if dest.exists() and dest.stat().st_size > 0:
        return dest
    dest.parent.mkdir(parents=True, exist_ok=True)
    url = RAW.format(owner=OWNER, repo=repo, branch=BRANCH, path=urllib.parse.quote(path))
    # curl вместо urllib: через прокси urllib нестабильно рвёт соединения
    result = subprocess.run([*CURL, "-o", str(dest), url], capture_output=True, text=True)
    if result.returncode == 0 and dest.exists() and dest.stat().st_size > 0:
        return dest
    error: Exception | None = None
    for attempt in range(5):
        try:
            req = urllib.request.Request(url, headers={"User-Agent": "collegelms-import"})
            with urllib.request.urlopen(req, timeout=180) as resp:
                dest.write_bytes(resp.read())
            return dest
        except Exception as err:  # noqa: BLE001
            error = err
            time.sleep(min(2**attempt, 20))
    raise RuntimeError(f"не скачался {repo}/{path}: {error or result.stderr.strip()}")


# --------------------------------------------------------------------------- #
# Разбор репозитория в занятия
# --------------------------------------------------------------------------- #
@dataclass
class Lesson:
    title: str
    kind: str
    content: str
    repo: str
    path: str  # путь в репозитории, нужен для разрешения относительных картинок
    files: list[tuple[str, Path]] = field(default_factory=list)  # (имя, локальный путь)
    images: dict[str, tuple[str, Path]] = field(default_factory=dict)  # ref -> (имя, путь)


def section_number(name: str) -> int:
    """«Раздел 5. React» и «5 Pandas» → 5; без номера — 10000."""
    match = re.match(r"(?:Раздел\s+)?(\d+)", name)
    return int(match.group(1)) if match else 10_000


def lesson_sort_key(name: str) -> tuple[int, int, str]:
    stem = os.path.splitext(name)[0]
    kind = kind_of(stem)
    match = re.search(r"(\d+)", stem)
    return (KIND_ORDER[kind], int(match.group(1)) if match else 0, stem)


def kind_of(stem: str, default: str = "Lecture") -> str:
    for prefix, kind in KIND_BY_PREFIX:
        if stem.startswith(prefix):
            return kind
    return default


def leading_number(name: str) -> int:
    match = re.match(r"\D*(\d+)", name)
    return int(match.group(1)) if match else 10_000


def drop_title_heading(content: str, title: str) -> str:
    """Убирает ведущий заголовок, если он повторяет название занятия."""
    lines = content.splitlines()
    index = 0
    while index < len(lines) and not lines[index].strip():
        index += 1
    if index >= len(lines):
        return content
    match = re.match(r"^#\s+(.*\S)\s*$", lines[index])
    if not match:
        return content
    heading = match.group(1)
    tail = title.split("·")[-1].strip()
    if heading.casefold() == tail.casefold():
        return "\n".join(lines[index + 1 :]).lstrip("\n")
    return content


def notebook_to_markdown(
    path: Path, image_dir: Path, image_prefix: str
) -> tuple[str, dict[str, tuple[str, Path]]]:
    """Ноутбук → markdown без вывода ячеек; картинки вывода сохраняются файлами."""
    notebook = json.loads(path.read_text(encoding="utf-8"))
    parts: list[str] = []
    images: dict[str, tuple[str, Path]] = {}
    counter = 0
    for cell in notebook.get("cells", []):
        source = "".join(cell.get("source", [])).strip()
        if not source:
            continue
        if cell.get("cell_type") == "code":
            parts.append("```python\n" + source + "\n```")
            for output in cell.get("outputs", []):
                image = (output.get("data") or {}).get("image/png")
                if not image:
                    continue
                counter += 1
                name = f"{image_prefix}-{counter}.png"
                target = image_dir / name
                target.parent.mkdir(parents=True, exist_ok=True)
                target.write_bytes(base64.b64decode(image))
                images[name] = (name, target)
                parts.append(f"![График из ноутбука]({name})")
        else:
            parts.append(source)
    return "\n\n".join(parts).strip() + "\n", images


def collect_lessons(repo: str, files: list[str], cache: Path, config: dict) -> list[Lesson]:
    layout = config.get("layout", "sections")
    if layout == "notebooks":
        return collect_notebook_lessons(repo, files, cache)
    return collect_section_lessons(repo, files, cache, config)


def collect_section_lessons(repo: str, files: list[str], cache: Path, config: dict) -> list[Lesson]:
    lessons: list[Lesson] = []
    course_files = [p for p in files if p.startswith("course/") and p.lower().endswith(".md")]
    loose = [p for p in course_files if p.count("/") == 1]
    sections: dict[str, list[str]] = {}
    for path in course_files:
        if path.count("/") < 2:
            continue
        section = path.split("/")[1]
        sections.setdefault(section, []).append(path)

    def sort_key(path: str) -> tuple[tuple[int, int, str], str]:
        return (lesson_sort_key(os.path.basename(path)), path)

    for path in sorted(loose, key=sort_key):
        local = download(repo, path, cache)
        stem = os.path.splitext(os.path.basename(path))[0]
        lessons.append(
            Lesson(
                title=stem,
                kind=kind_of(stem),
                content=drop_title_heading(local.read_text(encoding="utf-8"), stem),
                repo=repo,
                path=path,
            )
        )

    for section in sorted(sections, key=section_number):
        for path in sorted(sections[section], key=sort_key):
            local = download(repo, path, cache)
            stem = os.path.splitext(os.path.basename(path))[0]
            title, header = section_title(section, stem)
            content = drop_title_heading(local.read_text(encoding="utf-8"), title)
            if header:
                content = f"> **{header}**\n\n{content}"
            lessons.append(
                Lesson(
                    title=title,
                    kind=kind_of(stem, "Practice" if not header else "Lecture"),
                    content=content,
                    repo=repo,
                    path=path,
                )
            )

    for path, title in config.get("exam_lessons", []):
        if path not in files:
            continue
        local = download(repo, path, cache)
        lessons.append(
            Lesson(
                title=title,
                kind="SelfStudy",
                content=drop_title_heading(local.read_text(encoding="utf-8"), title),
                repo=repo,
                path=path,
            )
        )

    attach_sibling_files(lessons, files, cache)
    return lessons


def attach_sibling_files(lessons: list[Lesson], files: list[str], cache: Path) -> None:
    """Презентации и раздатки лежат рядом с лекцией: «Лекция 14. … (презентация).pptx»."""
    by_dir: dict[str, list[str]] = {}
    for path in files:
        if path.startswith("course/") and not path.lower().endswith((".md", *IMAGE_EXTS)):
            by_dir.setdefault(os.path.dirname(path), []).append(path)

    def key(stem: str) -> str | None:
        match = re.match(r"^((?:Лекция|Практическая работа|Самостоятельная работа)\s*\d+)", stem)
        return match.group(1) if match else None

    for lesson in lessons:
        stem = lesson.title.split("·")[-1].strip()
        token = key(stem)
        if not token:
            continue
        for path in by_dir.get(os.path.dirname(lesson.path), []):
            if key(os.path.basename(path)) == token:
                lesson.files.append((os.path.basename(path), download(lesson.repo, path, cache)))


GENERIC_STEMS = {"ipython", "notebook", "lesson", "урок", "практическая работа"}


def notebook_stem(stem: str, content: str) -> str:
    """«Lesson3 IPython» → «Командная оболочка IPython»: имя файла заменяем заголовком."""
    cleaned = re.sub(r"^Lesson\s*\d*\s*[.:]?\s*", "", stem, flags=re.IGNORECASE).strip()
    if cleaned and cleaned.casefold() not in GENERIC_STEMS:
        return cleaned
    for line in content.splitlines():
        heading = re.match(r"^#{1,3}\s+(.*\S)\s*$", line.strip())
        if heading:
            return heading.group(1)
    return stem


def readable_text(path: Path, limit: int = 20_000) -> str:
    """Текст из .txt/.htm/.html — без разметки, обрезанный до limit символов."""
    if path.suffix.lower() not in {".txt", ".htm", ".html"}:
        return ""
    raw = path.read_text(encoding="utf-8", errors="ignore")
    if path.suffix.lower() in {".htm", ".html"}:
        raw = re.sub(r"(?is)<(script|style)\b.*?</\1>", " ", raw)
        raw = re.sub(r"(?i)<br\s*/?>|</p>|</div>|</tr>|</li>|</h\d>", "\n", raw)
        raw = re.sub(r"<[^>]+>", " ", raw)
        raw = html.unescape(raw)
    text = re.sub(r"[ \t\xa0]+", " ", raw)
    text = re.sub(r"\n\s*\n\s*\n+", "\n\n", text).strip()
    if len(text) > limit:
        text = text[:limit].rsplit("\n", 1)[0] + "\n\n_Продолжение — в исходном файле._"
    return text


def section_title(section: str, stem: str) -> tuple[str, str]:
    """«Раздел 5. React» + «Лекция 1. …» → («Раздел 5 · Лекция 1. …», «Раздел 5. React»)."""
    number = section_number(section)
    if number == 10_000:
        return stem, ""
    name = re.sub(r"^(?:Раздел\s+)?\d+[.\s]*", "", section, count=1).strip() or section
    return f"Раздел {number} · {stem}", f"Раздел {number}. {name}"


def collect_notebook_lessons(repo: str, files: list[str], cache: Path) -> list[Lesson]:
    root_dir = "МДК 02.03 Математическое моделирование"
    in_course = [p for p in files if p.startswith(root_dir + "/")]
    lessons: list[Lesson] = []

    def sort_key(path: str) -> tuple[int, int, int, str]:
        relative = path[len(root_dir) + 1 :]
        section = relative.split("/")[0]
        name = os.path.basename(path)
        is_lecture = name.lower().startswith("lesson")
        return (section_number(section), 0 if is_lecture else 1, leading_number(name), path)

    for path in sorted([p for p in in_course if p.lower().endswith(".ipynb")], key=sort_key):
        local = download(repo, path, cache)
        section = path[len(root_dir) + 1 :].split("/")[0]
        raw_stem = os.path.splitext(os.path.basename(path))[0]
        image_dir = cache / repo / ".notebook-images" / raw_stem
        content, images = notebook_to_markdown(local, image_dir, raw_stem)
        stem = notebook_stem(raw_stem, content)
        title, header = section_title(section, stem)
        content = drop_title_heading(content, title)
        if header:
            content = f"> **{header}**\n\n{content}"
        lessons.append(
            Lesson(
                title=title,
                kind="Lecture" if os.path.basename(path).lower().startswith("lesson") else "Practice",
                content=content,
                repo=repo,
                path=path,
                files=[(os.path.basename(path), local)],
                images=images,
            )
        )

    for path in sorted([p for p in in_course if not p.lower().endswith(".ipynb")], key=sort_key):
        relative = path[len(root_dir) + 1 :]
        section = os.path.dirname(relative) if "/" in relative else relative
        local = download(repo, path, cache)
        stem = os.path.splitext(os.path.basename(path))[0]
        title, header = section_title(section, stem)
        body = f"> **{header}**\n\n" if header else ""
        text = readable_text(local)
        if text:
            body += f"# {stem}\n\n{text}\n\n_Исходный файл приложен к занятию: `{os.path.basename(path)}`._\n"
        else:
            body += f"# {stem}\n\nФайлы практической работы приложены к занятию: `{os.path.basename(path)}`.\n"
        lessons.append(
            Lesson(
                title=title,
                kind="Practice",
                content=body,
                repo=repo,
                path=path,
                files=[(os.path.basename(path), local)],
            )
        )
    return lessons


# --------------------------------------------------------------------------- #
# Обработка markdown: картинки и битые ссылки
# --------------------------------------------------------------------------- #
IMAGE_RE = re.compile(r"!\[([^\]]*)\]\(([^)\s]+)(?:\s+\"[^\"]*\")?\)")
LINK_RE = re.compile(r"(?<!!)\[([^\]]*)\]\(([^)\s]+)\)")


FENCE_RE = re.compile(r"^(\s*)(`{3,}|~{3,})")


def sub_outside_code(content: str, pattern: re.Pattern[str], repl) -> str:
    """Замены только в прозаических блоках: внутри ```кода``` ничего не трогаем."""
    out: list[str] = []
    fence: str | None = None
    for line in content.splitlines(keepends=True):
        match = FENCE_RE.match(line.rstrip("\n"))
        if fence is None and match:
            fence = match.group(2)[0] * 3
            out.append(line)
            continue
        if fence is not None:
            if match and match.group(2).startswith(fence):
                fence = None
            out.append(line)
            continue
        out.append(pattern.sub(repl, line))
    return "".join(out)


def resolve_images(lesson: Lesson, files: list[str], cache: Path) -> None:
    base = os.path.dirname(lesson.path)

    def replace(match: re.Match[str]) -> str:
        alt, ref = match.group(1).strip(), match.group(2).strip()
        if ref.startswith(("http://", "https://", "data:")):
            return match.group(0)
        target = os.path.normpath(os.path.join(base, urllib.parse.unquote(ref)))
        if os.path.splitext(target)[1].lower() in IMAGE_EXTS and target in files:
            local = download(lesson.repo, target, cache)
            lesson.images[ref] = (os.path.basename(target), local)
            return match.group(0)
        # картинки нет в репозитории — оставляем подпись, чтобы не было битых ссылок
        return f"> **Рисунок:** {alt or 'иллюстрация к заданию'}" if alt else ""

    lesson.content = sub_outside_code(lesson.content, IMAGE_RE, replace)
    lesson.content = re.sub(r"\n{3,}", "\n\n", lesson.content)


def check_links(lesson: Lesson, files: list[str]) -> list[str]:
    missing: list[str] = []
    base = os.path.dirname(lesson.path)
    for match in LINK_RE.finditer(lesson.content):
        ref = match.group(2).strip()
        if ref.startswith(("http://", "https://", "mailto:", "#")):
            continue
        target = os.path.normpath(os.path.join(base, urllib.parse.unquote(ref)))
        if target not in files:
            missing.append(ref)
    return missing


# --------------------------------------------------------------------------- #
# Клиент CollegeLMS
# --------------------------------------------------------------------------- #
class Lms:
    def __init__(self, base_url: str, token: str) -> None:
        self.base_url = base_url.rstrip("/")
        self.token = token

    def _request(self, method: str, path: str, **kwargs) -> object:
        import requests  # импорт внутри, чтобы --dry-run работал без зависимостей

        url = f"{self.base_url}{path}"
        headers = kwargs.pop("headers", {})
        headers["Authorization"] = f"Bearer {self.token}"
        for attempt in range(4):
            response = requests.request(method, url, headers=headers, timeout=300, **kwargs)
            if response.status_code < 500:
                break
            time.sleep(2 * (attempt + 1))
        if response.status_code >= 400:
            raise RuntimeError(f"{method} {url} -> {response.status_code}: {response.text[:400]}")
        if not response.content:
            return None
        payload = response.json()
        if isinstance(payload, dict) and payload.get("isSuccess") is False:
            raise RuntimeError(f"{method} {url} -> {payload}")
        return payload.get("data") if isinstance(payload, dict) else payload

    def groups(self) -> list[dict]:
        return self._request("GET", "/api/groups")  # type: ignore[return-value]

    def courses(self) -> list[dict]:
        return self._request("GET", "/api/courses")  # type: ignore[return-value]

    def create_course(self, title: str, description: str, teacher_id: str) -> dict:
        return self._request(  # type: ignore[return-value]
            "POST",
            "/api/courses",
            json={"title": title, "description": description, "teacherId": teacher_id},
        )

    def update_course(self, course_id: str, title: str, description: str, status: str) -> None:
        self._request(
            "PUT",
            f"/api/courses/{course_id}",
            json={"title": title, "description": description, "status": status, "authorIds": []},
        )

    def set_active(self, course_id: str, is_active: bool) -> None:
        self._request("PATCH", f"/api/courses/{course_id}/active", json={"isActive": is_active})

    def assign_groups(self, course_id: str, group_ids: list[str]) -> None:
        self._request("POST", f"/api/courses/{course_id}/groups", json={"groupIds": group_ids})

    def create_lesson(self, course_id: str, lesson: Lesson, after_lesson_id: str | None = None) -> str:
        # без afterLessonId API вставляет занятие в начало курса
        data = self._request(
            "POST",
            f"/api/courses/{course_id}/lessons",
            json={
                "title": lesson.title,
                "content": lesson.content,
                "kind": lesson.kind,
                "afterLessonId": after_lesson_id,
            },
        )
        return data["id"]  # type: ignore[index]

    def update_lesson(self, course_id: str, lesson_id: str, lesson: Lesson) -> None:
        self._request(
            "PUT",
            f"/api/courses/{course_id}/lessons/{lesson_id}",
            json={"title": lesson.title, "content": lesson.content, "kind": lesson.kind},
        )

    def upload_material(self, course_id: str, lesson_id: str, name: str, local: Path) -> str:
        path = os.path.join("materials", course_id, f"{uuid.uuid4()}_{os.path.basename(name)}")
        mime = mimetypes.guess_type(name)[0] or "application/octet-stream"
        data = self._request(
            "POST",
            f"/api/courses/{course_id}/materials?lessonId={lesson_id}",
            files={"file": (os.path.basename(name), local.read_bytes(), mime)},
        )
        return data["id"]  # type: ignore[index]

    def upload_document(self, course_id: str, name: str, local: Path) -> None:
        mime = mimetypes.guess_type(name)[0] or "application/octet-stream"
        self._request(
            "POST",
            f"/api/courses/{course_id}/documents",
            files={"file": (os.path.basename(name), local.read_bytes(), mime)},
        )


def material_url(material_id: str) -> str | None:
    """Путь загруженного файла API не отдаёт — берём его из БД (только чтение)."""
    if not all(os.environ.get(key) for key in ("DB_HOST", "DB_PORT", "DB_USER", "DB_NAME")):
        return None
    query = f"select file_path from course_materials where id = '{material_id}'"
    result = subprocess.run(
        [
            "psql",
            "-h",
            os.environ["DB_HOST"],
            "-p",
            os.environ["DB_PORT"],
            "-U",
            os.environ["DB_USER"],
            "-d",
            os.environ["DB_NAME"],
            "-tAc",
            query,
        ],
        capture_output=True,
        text=True,
        check=True,
    )
    path = result.stdout.strip()
    return f"/uploads/{path}" if path else None


def public_url(material_id: str) -> str:
    return material_url(material_id) or ""


# --------------------------------------------------------------------------- #
# Импорт
# --------------------------------------------------------------------------- #
def match_groups(names: list[str], all_groups: list[dict]) -> list[tuple[str, str]]:
    def normalize(value: str) -> str:
        return re.sub(r"[^а-яa-z0-9]", "", value.lower())

    found = []
    for name in names:
        for group in all_groups:
            if normalize(group["name"]) == normalize(name):
                found.append((name, group["id"]))
                break
        else:
            print(f"  ! группа «{name}» не найдена в LMS", file=sys.stderr)
    return found


def prefetch(repo: str, files: list[str], cache: Path, config: dict, workers: int = 6) -> int:
    """Качает всё нужное заранее: raw.githubusercontent нестабилен под серией запросов."""
    if config.get("layout") == "notebooks":
        wanted = [p for p in files if p.startswith("МДК 02.03")]
    else:
        wanted = [p for p in files if p.startswith("course/")]
    wanted += expand_documents(config.get("documents", []), files)
    wanted += [path for path, _ in config.get("exam_lessons", []) if path in files]
    wanted = sorted(set(wanted))

    done = 0
    failed: list[str] = []

    def worker(path: str) -> tuple[str, bool]:
        try:
            download(repo, path, cache)
            return path, True
        except Exception:  # noqa: BLE001
            return path, False

    with ThreadPoolExecutor(max_workers=workers) as pool:
        for path, ok in pool.map(worker, wanted):
            done += ok
            if not ok:
                failed.append(path)
    print(f"  скачано файлов: {done}/{len(wanted)}")
    for path in failed:
        print(f"  ! не скачался {path}", file=sys.stderr)
    return len(failed)


def import_course(lms: Lms, config: dict, cache: Path, teacher_id: str, all_groups: list[dict], dry_run: bool) -> dict:
    repo = config["repo"]
    print(f"\n=== {repo}: {config['title']}")
    files = repo_files(repo, cache)
    prefetch(repo, files, cache, config)
    lessons = collect_lessons(repo, files, cache, config)
    for lesson in lessons:
        resolve_images(lesson, files, cache)

    broken = {lesson.title: check_links(lesson, files) for lesson in lessons}
    broken = {title: refs for title, refs in broken.items() if refs}
    if broken:
        print(f"  ! не найдено в репозитории ссылок: {len(broken)}")

    limits = [lesson for lesson in lessons if len(lesson.content) > CONTENT_LIMIT]
    for lesson in limits:
        print(f"  ! контент длиннее {CONTENT_LIMIT} символов: {lesson.title} ({len(lesson.content)})")

    images = sum(len(lesson.images) for lesson in lessons)
    extras = sum(len(lesson.files) for lesson in lessons)
    documents = expand_documents(config.get("documents", []), files)
    print(f"  занятий: {len(lessons)}, картинок: {images}, файлов к занятиям: {extras}, документов: {len(documents)}")
    print(f"  группы: {', '.join(config['groups'])}")
    if dry_run:
        for lesson in lessons[:5]:
            print(f"    - [{lesson.kind}] {lesson.title} ({len(lesson.content)} симв.)")
        if len(lessons) > 5:
            print(f"    ... ещё {len(lessons) - 5}")
        return {}

    existing = [course for course in lms.courses() if course["title"] == config["title"]]
    if existing:
        print(f"  ! курс с таким названием уже есть ({existing[0]['id']}), пропуск")
        return {"skipped": True}

    course = lms.create_course(config["title"], config["description"], teacher_id)
    course_id = course["id"]
    print(f"  создан курс {course_id}")

    lesson_ids: list[str] = []
    previous: str | None = None
    for index, lesson in enumerate(lessons, start=1):
        lesson_id = lms.create_lesson(course_id, lesson, previous)
        lesson_ids.append(lesson_id)
        previous = lesson_id
        print(f"    {index}/{len(lessons)} {lesson.title}")

    # картинки из лекций: грузим и переписываем ссылки на публичный /uploads
    uploaded_images = 0
    for lesson, lesson_id in zip(lessons, lesson_ids, strict=True):
        if not lesson.images:
            continue
        for ref, (name, local) in list(lesson.images.items()):
            material_id = lms.upload_material(course_id, lesson_id, name, local)
            url = public_url(material_id)
            if not url:
                print(f"  ! не удалось получить путь файла {name} — ссылка оставлена как есть")
                continue
            lesson.images[ref] = (name, local)
            lesson.content = sub_outside_code(
                lesson.content, IMAGE_RE, lambda match, url=url: match.group(0) if match.group(2) != ref else f"![{match.group(1)}]({url})"
            )
            uploaded_images += 1
        lms.update_lesson(course_id, lesson_id, lesson)

    for lesson, lesson_id in zip(lessons, lesson_ids, strict=True):
        for name, local in lesson.files:
            lms.upload_material(course_id, lesson_id, name, local)
            print(f"    файл {name}")

    for name in documents:
        lms.upload_document(course_id, os.path.basename(name), download(repo, name, cache))
        print(f"    документ {os.path.basename(name)}")

    groups = match_groups(config["groups"], all_groups)
    if groups:
        lms.assign_groups(course_id, [group_id for _, group_id in groups])

    lms.update_course(course_id, config["title"], config["description"], "Active")
    lms.set_active(course_id, True)

    return {
        "courseId": course_id,
        "lessons": len(lessons),
        "images": uploaded_images,
        "documents": len(documents),
        "groups": [name for name, _ in groups],
    }


def expand_documents(patterns: list[str], files: list[str]) -> list[str]:
    result: list[str] = []
    for pattern in patterns:
        matched = sorted(p for p in files if fnmatch(p, pattern))
        result.extend(matched)
    return result


def fnmatch(name: str, pattern: str) -> bool:
    import fnmatch as module

    return module.fnmatch(name, pattern)


def main() -> int:
    parser = argparse.ArgumentParser(description="Импорт курсов МДК из GitHub в CollegeLMS")
    parser.add_argument("--cache", default="/tmp/mdk", help="каталог кэша скачанных репозиториев")
    parser.add_argument("--api", default=os.environ.get("LMS_API", "http://127.0.0.1:5026"))
    parser.add_argument("--token", default=os.environ.get("ADMIN_TOKEN", ""))
    parser.add_argument("--teacher-id", default=os.environ.get("LMS_TEACHER_ID", ""))
    parser.add_argument("--only", action="append", default=[], help="импортировать только указанные репозитории")
    parser.add_argument("--dry-run", action="store_true", help="только показать план, ничего не создавать")
    args = parser.parse_args()

    if not args.dry_run and (not args.token or not args.teacher_id):
        parser.error("нужны ADMIN_TOKEN и LMS_TEACHER_ID (или --dry-run)")

    cache = Path(args.cache)
    cache.mkdir(parents=True, exist_ok=True)

    selected = [c for c in COURSES if not args.only or c["repo"] in args.only]
    lms = None
    all_groups: list[dict] = []
    if not args.dry_run:
        lms = Lms(args.api, args.token)
        all_groups = lms.groups()

    summary = []
    for config in selected:
        summary.append(
            import_course(lms, config, cache, args.teacher_id, all_groups, args.dry_run)  # type: ignore[arg-type]
        )

    print("\n=== Итого")
    for config, result in zip(selected, summary, strict=True):
        print(f"  {config['repo']}: {result or 'dry-run'}")
    return 0


if __name__ == "__main__":
    sys.exit(main())