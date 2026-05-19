"""
Fetch Missing VRS Operator Logos
================================
Reads the MissingLogos.log file, checks which operator codes still lack
a .bmp in the VRS flags folder, and fills them in:
  1. From the rikgale/VRSOperatorFlags community pack on GitHub (cached zip)
  2. As a text-based placeholder if the code isn't in the pack

Run any time to pick up newly logged missing codes.

Usage:
    python fetch_missing_logos.py [--force-update]

    --force-update   Re-download the community zip even if cached
"""

import os
import sys
import zipfile
import urllib.request
import urllib.error
from datetime import datetime, timedelta

# --- Configuration -----------------------------------------------------------
# Path to the MissingLogos.log file written by the VRS plugin
MISSING_LOG = r"C:\Program Files\VirtualRadar\Plugins\MissingLogos\MissingLogos.log"

# Folder where VRS looks for operator flag BMPs
FLAGS_FOLDER = r"C:\Users\Owner\Desktop\VRS\flags"

# Where to cache the community operator flags zip
CACHE_DIR = os.path.join(os.path.dirname(os.path.abspath(__file__)), "logos_cache")
CACHED_ZIP = os.path.join(CACHE_DIR, "OperatorFlags.zip")

# GitHub URL for the rikgale community flags pack
COMMUNITY_ZIP_URL = "https://github.com/rikgale/VRSOperatorFlags/raw/main/OperatorFlags.zip"

# Re-download the zip if older than this many days
CACHE_MAX_AGE_DAYS = 30

# --- End Configuration -------------------------------------------------------


def read_missing_codes(log_path):
    """Parse the MissingLogos.log file and return a set of ICAO codes."""
    codes = set()
    if not os.path.exists(log_path):
        print(f"  Log file not found: {log_path}")
        return codes
    with open(log_path, "r") as f:
        for line in f:
            parts = line.split("|")
            if len(parts) >= 2:
                code = parts[1].strip()
                if code:
                    codes.add(code)
    return codes


def find_still_missing(codes, flags_folder):
    """Return codes that don't have a .bmp in the flags folder yet."""
    missing = set()
    for code in codes:
        bmp_path = os.path.join(flags_folder, code + ".bmp")
        if not os.path.exists(bmp_path):
            missing.add(code)
    return missing


def download_community_zip(force=False):
    """Download the rikgale community flags zip, with caching."""
    os.makedirs(CACHE_DIR, exist_ok=True)

    if os.path.exists(CACHED_ZIP) and not force:
        age = datetime.now() - datetime.fromtimestamp(os.path.getmtime(CACHED_ZIP))
        if age < timedelta(days=CACHE_MAX_AGE_DAYS):
            print(f"  Using cached zip ({age.days} days old, max {CACHE_MAX_AGE_DAYS})")
            return True
        else:
            print(f"  Cached zip is {age.days} days old, re-downloading...")

    print(f"  Downloading community flags pack...")
    print(f"    {COMMUNITY_ZIP_URL}")
    try:
        urllib.request.urlretrieve(COMMUNITY_ZIP_URL, CACHED_ZIP)
        size_mb = os.path.getsize(CACHED_ZIP) / (1024 * 1024)
        print(f"    Downloaded {size_mb:.1f} MB")
        return True
    except urllib.error.URLError as e:
        print(f"    Download failed: {e}")
        return False


def get_zip_contents(zip_path):
    """Return a set of filenames (without path) in the zip."""
    try:
        with zipfile.ZipFile(zip_path, "r") as zf:
            return {os.path.basename(n) for n in zf.namelist()}
    except (zipfile.BadZipFile, FileNotFoundError):
        return set()


def extract_from_zip(zip_path, code, dest_folder):
    """Extract a single CODE.bmp from the zip into dest_folder."""
    filename = code + ".bmp"
    try:
        with zipfile.ZipFile(zip_path, "r") as zf:
            # Find the entry - might be in root or a subfolder
            for name in zf.namelist():
                if os.path.basename(name) == filename:
                    data = zf.read(name)
                    dest = os.path.join(dest_folder, filename)
                    with open(dest, "wb") as f:
                        f.write(data)
                    return True
    except (zipfile.BadZipFile, KeyError):
        pass
    return False


def create_placeholder_logo(code, dest_folder):
    """Create a simple 85x20 24-bit BMP with the ICAO code as text."""
    try:
        from PIL import Image, ImageDraw, ImageFont
    except ImportError:
        print(f"    Pillow not installed - cannot create placeholder for {code}")
        print(f"    Install with: pip install Pillow")
        return False

    img = Image.new("RGB", (85, 20), (255, 255, 255))
    draw = ImageDraw.Draw(img)

    try:
        font_code = ImageFont.truetype("arial.ttf", 14)
    except (OSError, IOError):
        font_code = ImageFont.load_default()

    # Center the ICAO code in the image
    bbox = draw.textbbox((0, 0), code, font=font_code)
    text_w = bbox[2] - bbox[0]
    text_h = bbox[3] - bbox[1]
    x = (85 - text_w) // 2
    y = (20 - text_h) // 2 - 1
    draw.text((x, y), code, fill=(0, 0, 128), font=font_code)

    # Thin border
    draw.rectangle([0, 0, 84, 19], outline=(200, 200, 200))

    dest = os.path.join(dest_folder, code + ".bmp")
    img.save(dest, "BMP")
    return True


def main():
    force_update = "--force-update" in sys.argv

    print("=== VRS Missing Logos Fetcher ===")
    print()

    # Validate paths
    if not os.path.isdir(FLAGS_FOLDER):
        print(f"ERROR: Flags folder not found: {FLAGS_FOLDER}")
        print("       Update FLAGS_FOLDER in the script if your VRS flags are elsewhere.")
        sys.exit(1)

    # Step 1: Read the missing logos log
    print(f"1. Reading missing logos log...")
    print(f"   {MISSING_LOG}")
    all_codes = read_missing_codes(MISSING_LOG)
    if not all_codes:
        print("   No missing codes found in log. Nothing to do.")
        return
    print(f"   Found {len(all_codes)} codes in log")

    # Step 2: Check which are still missing from the flags folder
    print(f"\n2. Checking flags folder for existing logos...")
    print(f"   {FLAGS_FOLDER}")
    missing = find_still_missing(all_codes, FLAGS_FOLDER)
    if not missing:
        print("   All codes already have logos! Nothing to do.")
        return
    print(f"   {len(missing)} codes still need logos: {', '.join(sorted(missing))}")

    # Step 3: Download / use cached community pack
    print(f"\n3. Community flags pack (rikgale/VRSOperatorFlags)...")
    have_zip = download_community_zip(force=force_update)

    zip_contents = set()
    if have_zip:
        zip_contents = get_zip_contents(CACHED_ZIP)
        print(f"   Pack contains {len(zip_contents)} files")

    # Step 4: Extract available logos from the community pack
    from_pack = set()
    placeholders = set()

    print(f"\n4. Fetching logos for {len(missing)} missing codes...")
    for code in sorted(missing):
        target_file = code + ".bmp"
        if target_file in zip_contents:
            if extract_from_zip(CACHED_ZIP, code, FLAGS_FOLDER):
                from_pack.add(code)
                print(f"   [PACK]        {code}")
            else:
                print(f"   [PACK-FAIL]   {code} - extraction error")
        else:
            if create_placeholder_logo(code, FLAGS_FOLDER):
                placeholders.add(code)
                print(f"   [PLACEHOLDER] {code}")
            else:
                print(f"   [SKIP]        {code} - could not create")

    # Summary
    print(f"\n=== Done ===")
    print(f"   From community pack: {len(from_pack)}")
    print(f"   Placeholders created: {len(placeholders)}")
    total = len(from_pack) + len(placeholders)
    still_missing = len(missing) - total
    if still_missing > 0:
        print(f"   Still missing: {still_missing}")
    print(f"   Logos folder: {FLAGS_FOLDER}")


if __name__ == "__main__":
    main()
