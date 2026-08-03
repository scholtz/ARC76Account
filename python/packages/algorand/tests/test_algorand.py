from arc76.algorand import get_account, get_email_account

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
