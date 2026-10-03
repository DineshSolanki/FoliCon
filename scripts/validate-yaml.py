#!/usr/bin/env python3
"""
Validates YAML files across the repository or a specific list of staged files.
Usage:
    python scripts/validate-yaml.py                  # Validates all repo YAML files
    python scripts/validate-yaml.py file1.yml ...   # Validates specific files
"""

import glob
import os
import sys

try:
    import yaml
except ImportError:
    print("[WARNING] PyYAML not installed. Attempting to install...")
    import subprocess
    subprocess.check_call([sys.executable, "-m", "pip", "install", "pyyaml", "-q"])
    import yaml

def validate_yaml_file(filepath):
    if not os.path.exists(filepath):
        return True
    try:
        with open(filepath, "r", encoding="utf-8") as f:
            yaml.safe_load(f)
        print(f"  [VALID] {filepath}")
        return True
    except yaml.YAMLError as exc:
        print(f"  [ERROR] {filepath}")
        if hasattr(exc, "problem_mark"):
            mark = exc.problem_mark
            print(f"    Line {mark.line + 1}, Column {mark.column + 1}: {exc.problem}")
            if exc.context:
                print(f"    Context: {exc.context}")
        else:
            print(f"    {exc}")
        return False
    except Exception as e:
        print(f"  [ERROR] {filepath}: {e}")
        return False

def main():
    if len(sys.argv) > 1:
        files = sys.argv[1:]
    else:
        files = glob.glob(".github/**/*.yml", recursive=True) + \
                glob.glob(".github/**/*.yaml", recursive=True) + \
                glob.glob("distribution/**/*.yaml", recursive=True)

    yaml_files = [f for f in files if f.endswith((".yml", ".yaml"))]

    if not yaml_files:
        print("No YAML files to validate.")
        return 0

    print(f"[INFO] Validating {len(yaml_files)} YAML file(s)...")
    failed = []

    for filepath in sorted(yaml_files):
        if not validate_yaml_file(filepath):
            failed.append(filepath)

    if failed:
        print(f"\n[ERROR] {len(failed)} YAML file(s) failed validation:")
        for f in failed:
            print(f"  - {f}")
        return 1

    print(f"\n[SUCCESS] All {len(yaml_files)} YAML file(s) passed validation!")
    return 0

if __name__ == "__main__":
    sys.exit(main())
