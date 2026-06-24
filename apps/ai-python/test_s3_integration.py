"""
test_s3_integration.py
Run this from inside apps/ai-python/ to verify S3 + config are wired correctly.

    cd apps/ai-python
    python test_s3_integration.py

Expected output: all lines show ✅
"""

import sys

# ── 1. Config import ──────────────────────────────────────────────────────
print("\n[1/5] Loading config...")
try:
    from app.config import settings, get_s3_service
    print("  ✅ config loaded")
    print(f"     aws_region    = {settings.aws_region}")
    print(f"     aws_s3_bucket = {settings.aws_s3_bucket}")
except Exception as e:
    print(f"  ❌ config import failed: {e}")
    sys.exit(1)

# ── 2. S3Service import ───────────────────────────────────────────────────
print("\n[2/5] Importing S3Service...")
try:
    s3 = get_s3_service()
    print(f"  ✅ S3Service instantiated — bucket={s3.bucket}")
except Exception as e:
    print(f"  ❌ S3Service import failed: {e}")
    sys.exit(1)

# ── 3. S3 connectivity ping ───────────────────────────────────────────────
print("\n[3/5] Pinging S3 bucket (write + delete test)...")
try:
    ok = s3.ping()
    if ok:
        print(f"  ✅ S3 bucket reachable — {s3.bucket}")
    else:
        print("  ❌ S3 ping returned False — check AWS credentials and bucket name")
        print("     Make sure these env vars are set:")
        print("       AWS_ACCESS_KEY_ID")
        print("       AWS_SECRET_ACCESS_KEY")
        sys.exit(1)
except Exception as e:
    print(f"  ❌ S3 ping exception: {e}")
    sys.exit(1)

# ── 4. GeoJSON upload test ────────────────────────────────────────────────
print("\n[4/5] Uploading test GeoJSON asset...")
try:
    test_parcel_id = "test_verification_parcel"
    test_geojson = {
        "type": "FeatureCollection",
        "features": [
            {
                "type": "Feature",
                "geometry": {"type": "Point", "coordinates": [31.23, 30.06]},
                "properties": {"test": True, "description": "GeoSense S3 verification"}
            }
        ]
    }
    uri = s3.upload_geojson(test_geojson, test_parcel_id, "test_contours.geojson")
    print("  ✅ GeoJSON uploaded")
    print(f"     URI: {uri}")
except Exception as e:
    print(f"  ❌ GeoJSON upload failed: {e}")
    sys.exit(1)

# ── 5. PNG tile upload test ───────────────────────────────────────────────
print("\n[5/5] Uploading test PNG tile...")
try:
    # Minimal 1x1 valid PNG (67 bytes)
    minimal_png = (
        b'\x89PNG\r\n\x1a\n'          # PNG signature
        b'\x00\x00\x00\rIHDR'         # IHDR chunk length + type
        b'\x00\x00\x00\x01'           # width = 1
        b'\x00\x00\x00\x01'           # height = 1
        b'\x08\x02'                    # 8-bit RGB
        b'\x00\x00\x00'               # flags
        b'\x90wS\xde'                  # CRC
        b'\x00\x00\x00\x0cIDATx'      # IDAT chunk
        b'\x9cc\xf8\x0f\x00\x00\x01'
        b'\x01\x00\x05\x18\xd8N'
        b'\x00\x00\x00\x00IEND'       # IEND chunk
        b'\xaeB`\x82'                  # CRC
    )
    tile_uri = s3.upload_tile(minimal_png, test_parcel_id, "elevation", z=14, x=9721, y=6142)
    print("  ✅ PNG tile uploaded")
    print(f"     URI: {tile_uri}")
    print(f"     Template: {s3.tile_url_template(test_parcel_id, 'elevation')}")
except Exception as e:
    print(f"  ❌ PNG tile upload failed: {e}")
    sys.exit(1)

# ── Summary ───────────────────────────────────────────────────────────────
print("\n" + "="*55)
print("✅ All checks passed — S3 integration is working")
print("="*55)
print("\nAssets will be stored under:")
print(f"  s3://{s3.bucket}/tiles/{{parcel_id}}/{{layer}}/{{z}}/{{x}}/{{y}}.png")
print(f"  s3://{s3.bucket}/assets/{{parcel_id}}/{{filename}}")
print("\nYou can verify the test files in your S3 console:")
print(f"  Bucket : {s3.bucket}")
print(f"  Region : {s3.region}")
print("  Path   : tiles/test_verification_parcel/  &  assets/test_verification_parcel/")
print()