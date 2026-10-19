from arc76.algorand import (
    get_account,
    get_email_account,
    get_email_falcon_account,
    get_falcon_account,
)

PASSWORD = "12345678901234567890123456789012345678901234567890"
EMAIL = "email@example.com"


# Known-answer values shared with dotnet/TestARC76/AVMTests.cs and
# typescript/packages/algorand/src/index.test.ts - if these ever diverge, cross-language ARC76
# compatibility has broken.
class TestGetAccount:
    def test_matches_known_password_derived_address(self) -> None:
        acc = get_account(PASSWORD)
        assert acc.address == "GIMN3RFDRRWFD7KIFSVRMXC4JXYD5ZXKT7KSN57OG4RAZPPC3L3YDM46NQ"

    def test_matches_known_slot_1_address(self) -> None:
        acc = get_account(PASSWORD, 1)
        assert acc.address == "AMOLO4ICDKCOLFVJXAVIQ23TA27P3LW4AHJP54LLNV4JPIAFKBYLH3SGVQ"


class TestGetEmailAccount:
    def test_matches_known_email_password_derived_address(self) -> None:
        acc = get_email_account(EMAIL, PASSWORD)
        assert acc.address == "5AHWQJ5D52K4GRW4JWQ5GMR53F7PDSJEGT4PXVFSBQYE7VXDVG3WSPWSBM"


# Falcon-1024 known-answer values shared with dotnet/TestARC76/AVMTests.cs and
# typescript/packages/algorand/src/index.test.ts.
PARITY = [
    (6, "SQUKPUF5WNY2Y5VWPKEDVM2KGH2LPML6PIQMOFIRBTMHJTEC4BXLEPM4XI", 4),
    (7, "LWTOIX3ZCF4UDNFTS4A5S7436MT3QTFIFVSLQGVLODOYTVT6BVUUKGI2NU", 1),
    (9, "U6MYURHZFMNYLTUTOLBAHD7LNMT4WQ572GUHZOW7RJFGD6CYVDJNHDY2AU", 6),
    (18, "QR3WQTKMSN6IW7SIM3OD3WXA4G57AG3YOBKTKH4R2UAU32KABR2ZA4LMD4", 2),
]


class TestFalconAccount:
    def test_matches_known_password_derived_address(self) -> None:
        acc = get_falcon_account(PASSWORD)
        assert acc.address == "BGIJDJONAAYPYPJSZX6EWMH24VMU7CDAOUNCPCIXQ4KYYJG3QMXWNVWOWE"

    def test_matches_known_email_password_derived_address(self) -> None:
        acc = get_email_falcon_account(EMAIL, PASSWORD)
        assert acc.address == "TD5NFW5MEPGAT4S2AIN6XK5DIPJQMCOVN3272HJUJPPG3TB52MNFESSRXA"

    def test_matches_known_slot_1_address(self) -> None:
        acc = get_falcon_account(PASSWORD, 1)
        assert acc.address == "CV2LVHMWDQ625SKQOWQHYZN453MZ22R3V2DO5HFU6UDUZVDKGZJES4HFC4"

    def test_differs_from_ed25519_and_signs(self) -> None:
        from algorand_falcon.falcon1024 import Signer

        acc = get_falcon_account(PASSWORD)
        assert acc.address != get_account(PASSWORD).address
        assert len(acc.public_key) == 1793 and len(acc.private_key) == 2305
        signer = Signer(acc.private_key)
        assert signer.verifying_key().is_valid(b"hi", signer.sign(b"hi"))

    def test_parity_vectors_cover_several_salts(self) -> None:
        for slot, address, salt in PARITY:
            acc = get_falcon_account(f"parity-password-{slot}", slot)
            assert (acc.address, acc.salt) == (address, salt)
