import os
import re
import json

def extract_keys_from_dart(directory):
    pattern = re.compile(r"'(?:[^'\\]|\\.)*'|\"(?:[^\"\\]|\\.)*\"")
    # Match strings followed by .tr
    keys = set()
    for root, _, files in os.walk(directory):
        for file in files:
            if file.endswith('.dart'):
                with open(os.path.join(root, file), 'r', encoding='utf-8') as f:
                    content = f.read()
                    # A simplistic approach: find all occurrences of .tr() and look back at the string before it
                    # E.g. 'Hello'.tr()
                    matches = re.findall(r"('([^'\\]*(?:\\.[^'\\]*)*)'|\"([^\"\\]*(?:\\.[^\"\\]*)*)\")\.tr", content)
                    for match in matches:
                        if match[1]:
                            keys.add(match[1])
                        elif match[2]:
                            keys.add(match[2])
    return keys

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
    mobile_dir = r"d:\LAGBAR SHOP\OnlineStore_updated\OnlineStore\mobile\online_store_mobile\lib"
    web_dir = r"d:\LAGBAR SHOP\OnlineStore_updated\OnlineStore\web\online-store-web\src"

    dart_keys = extract_keys_from_dart(mobile_dir)
    print(f"Found {len(dart_keys)} keys in Dart.")

    jsx_keys = extract_keys_from_jsx(web_dir)
    print(f"Found {len(jsx_keys)} keys in JSX.")

    all_keys = dart_keys.union(jsx_keys)
    print(f"Total unique keys: {len(all_keys)}")

    mobile_locales = [
        r"d:\LAGBAR SHOP\OnlineStore_updated\OnlineStore\mobile\online_store_mobile\assets\translations\en.json",
        r"d:\LAGBAR SHOP\OnlineStore_updated\OnlineStore\mobile\online_store_mobile\assets\translations\ar.json",
        r"d:\LAGBAR SHOP\OnlineStore_updated\OnlineStore\mobile\online_store_mobile\assets\translations\tr.json"
    ]

    web_locales = [
        r"d:\LAGBAR SHOP\OnlineStore_updated\OnlineStore\web\online-store-web\public\locales\en\translation.json",
        r"d:\LAGBAR SHOP\OnlineStore_updated\OnlineStore\web\online-store-web\public\locales\ar\translation.json",
        r"d:\LAGBAR SHOP\OnlineStore_updated\OnlineStore\web\online-store-web\public\locales\tr\translation.json"
    ]

    for path in mobile_locales + web_locales:
        added = update_json_file(path, all_keys)
        print(f"Added {added} keys to {path}")

if __name__ == '__main__':
    main()
