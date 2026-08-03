from arc76.bitcoincash import get_account, get_email_account

PASSWORD = "12345678901234567890123456789012345678901234567890"
EMAIL = "email@example.com"


# Known-answer values shared with dotnet/TestARC76/BitcoinCashTests.cs and
# typescript/packages/bitcoincash/src/index.test.ts - if these ever diverge, cross-language
# ARC76 compatibility has broken.
class TestGetAccount:
    def test_matches_known_password_derived_address_and_wif(self) -> None:
        account = get_account(PASSWORD)
        assert account.address() == "bitcoincash:qr34393e7nat5476magek26aul9q3w3lxc4hph4ljz"
        assert account.wif() == "L2AEXy6SztUrAa6bMZLyamdbF6xdALS6pUWBct6JKRLz9xDsm57Z"

    def test_matches_known_slot_1_address_and_wif(self) -> None:
        account = get_account(PASSWORD, 1)
        assert account.address() == "bitcoincash:qpllmp6d3d9ewjya3s5s08ssvmnhdr7heuv0dvkuds"
        assert account.wif() == "KykJVUYMpC2xCrUb8dENcRP3cvQHt6BQCskrkJvyW7fvGEVxpgWA"


class TestGetEmailAccount:
    def test_matches_known_email_password_derived_address_and_wif(self) -> None:
        account = get_email_account(EMAIL, PASSWORD)
        assert account.address() == "bitcoincash:qp6fcyedrugd3l2d4yvzptn9p00hdqup5g7zx5pytw"
        assert account.wif() == "L4m915x48Hasve5Dzc2BnJuUaxX85xWeF8PtUo5dP234VtSZjDtL"
