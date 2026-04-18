import os
import json
import zipfile
import datetime
import subprocess

INSTANCE_PATH = r"C:\Users\Leandro\AppData\Roaming\KrakenLauncher\instances\4071f9b86f5244d690151d51524248bb"
REPO_PATH = r"c:\Users\Leandro\source\repos\NebulaLauncher"
NEXT_VERSION = "1.0.29"
OLD_VERSION = "1.0.28"

def run_cmd(args, cwd=REPO_PATH):
    print(f"Running: {' '.join(args)}")
    res = subprocess.run(args, cwd=cwd, capture_output=True, text=True)
    if res.returncode != 0:
        print(f"Error: {res.stderr}")
    return res.returncode

def publish():
    # 0. Sync local
    run_cmd(["git", "pull", "origin", "main", "--rebase"])

    # 1. Zip assets
    zip_path = os.path.join(REPO_PATH, "client-assets.zip")
    with zipfile.ZipFile(zip_path, 'w', zipfile.ZIP_DEFLATED) as zipf:
        zipf.write(os.path.join(INSTANCE_PATH, "options.txt"), "options.txt")
        for folder in ["config", "shaderpacks", "resourcepacks", "scripts"]:
            src_folder = os.path.join(INSTANCE_PATH, folder)
            if os.path.exists(src_folder):
                for root, dirs, files in os.walk(src_folder):
                    for file in files:
                        full_path = os.path.join(root, file)
                        rel_path = os.path.relpath(full_path, INSTANCE_PATH)
                        zipf.write(full_path, rel_path)

    # 2. Release to GitHub
    tag = f"v{NEXT_VERSION}-assets"
    run_cmd(["gh", "release", "create", tag, zip_path, "--repo", "leaboga/nebula-modpack", "--title", f"Assets v{NEXT_VERSION}", "--notes", "Fix: Sincronización dinámica de assets implementada."])

    # 3. Update manifest
    old_manifest_path = os.path.join(REPO_PATH, "versions", OLD_VERSION, "manifest.json")
    new_version_dir = os.path.join(REPO_PATH, "versions", NEXT_VERSION)
    os.makedirs(new_version_dir, exist_ok=True)
    new_manifest_path = os.path.join(new_version_dir, "manifest.json")

    with open(old_manifest_path, 'r', encoding='utf-8') as f:
        manifest = json.load(f)

    now = datetime.datetime.now()
    timestamp = now.strftime("%d/%m/%Y %H:%M")
    
    manifest["version"] = NEXT_VERSION
    manifest["configVersion"] = f"{NEXT_VERSION} ({timestamp})"
    manifest["configHash"] = str(int(now.timestamp() * 10000000))
    manifest["forceConfigUpdate"] = True

    with open(new_manifest_path, 'w', encoding='utf-8') as f:
        json.dump(manifest, f, indent=4)

    # 4. Update index
    index_path = os.path.join(REPO_PATH, "versions-index.json")
    with open(index_path, 'r', encoding='utf-8') as f:
        index = json.load(f)

    index["latestVersion"] = NEXT_VERSION
    new_entry = {
        "version": NEXT_VERSION,
        "label": f"v{NEXT_VERSION} ({timestamp})",
        "manifestUrl": f"https://raw.githubusercontent.com/leaboga/nebula-modpack/main/versions/{NEXT_VERSION}/manifest.json"
    }
    index["availableVersions"].insert(0, new_entry)

    with open(index_path, 'w', encoding='utf-8') as f:
        json.dump(index, f, indent=4)

    # 4.5 Update config-hash.json
    hash_path = os.path.join(REPO_PATH, "config-hash.json")
    with open(hash_path, 'w', encoding='utf-8') as f:
        json.dump({"hash": NEXT_VERSION}, f, indent=4)

    # 5. Git push (CLEAN)
    if os.path.exists(zip_path):
        os.remove(zip_path) # DELETE ZIP BEFORE ADD .

    run_cmd(["git", "add", "."])
    run_cmd(["git", "commit", "-m", f"Auto-publish v{NEXT_VERSION} - Sincronización dinámica activada"])
    run_cmd(["git", "push", "origin", "main"])

if __name__ == "__main__":
    publish()
