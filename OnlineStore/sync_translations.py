import os
import re
import json

SCRIPT_DIR = os.path.dirname(os.path.abspath(__file__))
WEB_DIR = os.path.join(SCRIPT_DIR, "web", "online-store-web")


def extract_keys_from_jsx(directory):
    keys = set()
    for root, _, files in os.walk(directory):
        for file in files:
            if file.endswith(('.jsx', '.js')):
                with open(os.path.join(root, file), 'r', encoding='utf-8') as f:
                    content = f.read()
                    matches = re.findall(r"t\(\s*('([^'\\]*(?:\\.[^'\\]*)*)'|\"([^\"\\]*(?:\\.[^\"\\]*)*)\")", content)
                    for match in matches:
                        if match[1]:
                            keys.add(match[1])
                        elif match[2]:
                            keys.add(match[2])
    return keys


def update_json_file(filepath, keys):
    if os.path.exists(filepath):
        with open(filepath, 'r', encoding='utf-8') as f:
            try:
                data = json.load(f)
            except:
                data = {}
    else:
        data = {}
        os.makedirs(os.path.dirname(filepath), exist_ok=True)

    added = 0
    for key in keys:
        if key not in data:
            data[key] = key  # Default to english/key itself
            added += 1

    with open(filepath, 'w', encoding='utf-8') as f:
        json.dump(data, f, ensure_ascii=False, indent=2)
    return added


def main():
    src_dir = os.path.join(WEB_DIR, "src")

    jsx_keys = extract_keys_from_jsx(src_dir)
    print(f"Found {len(jsx_keys)} keys in JSX.")

    web_locales = [
        os.path.join(WEB_DIR, "public", "locales", lang, "translation.json")
        for lang in ("en", "ar", "tr")
    ]

    for path in web_locales:
        added = update_json_file(path, jsx_keys)
        print(f"Added {added} keys to {path}")


if __name__ == '__main__':
    main()
