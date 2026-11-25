"""Independent, audit-only release preflight. No approval authority is enrolled.
A valid input never authorizes publishing. Run from independently trusted checkout.
Exit 78 = blocked; 65 = invalid evidence; argparse 2 = bad invocation.
"""
import argparse
import hashlib
import json
import os
import pathlib
import re
import stat
import sys

PRODUCT_HEAD = "9f9af1fb33fb0f69c6d3f1f408e467a856e4ca0f"
DOCUMENT_SHA = "83db400dc7a079f256eb1a37e8dece3e58ee207a358d022a4f114c999a0ec455"
# Trust is not supplied by package, caller, environment or an unsigned record.
# A future separately reviewed policy change must enroll authority and implement
# verification (signer scope, expiry, revocation and package/HEAD binding).
ENROLLED_APPROVAL_AUTHORITIES = ()
BLOCKERS = (
    "SDK_EULA_HISTORICAL_BINDING", "GPL_MICROSOFT_COMPATIBILITY",
    "COPYRIGHT_HOLDER_AUTHORIZATION", "LOGO_REDISTRIBUTION_RIGHTS",
    "OFFICIAL_LEGAL_SOURCE", "PHYSICAL_WINDOWS_UAT",
    "DOWNSTREAM_TERMS_REVIEW", "FINAL_PRODUCT_PACKAGE_APPROVAL",
)
MAX_PACKAGE = 512 * 1024 * 1024
MAX_RECORD = 64 * 1024

def fingerprint(path, maximum):
    p = pathlib.Path(path)
    if p.is_symlink() or not p.is_file():
        raise ValueError("not a regular input file")
    h = hashlib.sha256()
    with p.open("rb") as f:
        a = os.fstat(f.fileno())
        if not stat.S_ISREG(a.st_mode) or not 0 < a.st_size <= maximum:
            raise ValueError("input size/type")
        total = 0
        while True:
            b = f.read(1024 * 1024)
            if not b:
                break
            total += len(b)
            if total > maximum:
                raise ValueError("input too large")
            h.update(b)
        z = os.fstat(f.fileno())
        if (a.st_size, a.st_mtime_ns, a.st_ctime_ns) != (z.st_size, z.st_mtime_ns, z.st_ctime_ns) or total != a.st_size:
            raise ValueError("input changed during read")
    return {"sha256": h.hexdigest(), "bytes": total}

def evaluate(args):
    result = {"schema": 1, "mode": "AUDIT_ONLY", "release_allowed": False,
              "product_head": PRODUCT_HEAD, "open_gates": list(BLOCKERS),
              "enrolled_authority_count": len(ENROLLED_APPROVAL_AUTHORITIES),
              "approval_status": "NO_TRUST_ROOT", "package_kind": args.kind}
    try:
        if args.product_head != PRODUCT_HEAD:
            raise ValueError("product HEAD mismatch")
        if not re.fullmatch(r"[0-9a-f]{64}", args.expected_sha256):
            raise ValueError("invalid expected digest")
        if args.kind == "documents" and args.expected_sha256 != DOCUMENT_SHA:
            raise ValueError("document digest not pinned in trusted code")
        p = fingerprint(args.package, MAX_PACKAGE)
        result["package"] = p
        if p["sha256"] != args.expected_sha256:
            raise ValueError("candidate digest mismatch")
        if args.approval_record:
            result["untrusted_record"] = fingerprint(args.approval_record, MAX_RECORD)
            result["approval_status"] = "UNTRUSTED_RECORD_NO_ENROLLED_AUTHORITY"
        result["integrity"] = "PINNED_DOCUMENT_MATCH" if args.kind == "documents" else "CALLER_DIGEST_MATCH_ONLY"
        result["status"] = "DENY_NO_ENROLLED_AUTHORITY"
        return 78, result
    except (OSError, ValueError) as e:
        result.update(status="DENY_INVALID_INPUT", reason=str(e), integrity="FAIL")
        return 65, result

def main():
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument("--package", required=True)
    p.add_argument("--kind", choices=["documents", "product"], required=True)
    p.add_argument("--product-head", required=True)
    p.add_argument("--expected-sha256", required=True)
    p.add_argument("--approval-record", help="Untrusted record fingerprint only; never accepted as authorization")
    code, result = evaluate(p.parse_args())
    print(json.dumps(result, sort_keys=True))
    return code

if __name__ == "__main__":
    sys.exit(main())
