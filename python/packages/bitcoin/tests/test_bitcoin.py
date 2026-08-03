from arc76.bitcoin import get_account, get_email_account

PASSWORD = "12345678901234567890123456789012345678901234567890"
EMAIL = "email@example.com"


# Known-answer values shared with dotnet/TestARC76/BitcoinTests.cs and
# typescript/packages/bitcoin/src/index.test.ts - if these ever diverge, cross-language ARC76
# compatibility has broken.
class TestGetAccount:
    def test_matches_known_password_derived_address_and_wif(self) -> None:
        account = get_account(PASSWORD)
        assert account.address() == "bc1qudvfvw05l2a90kkl2xdjkh08egyt50ekkprkas"
        assert account.wif() == "L2AEXy6SztUrAa6bMZLyamdbF6xdALS6pUWBct6JKRLz9xDsm57Z"

    def test_matches_known_slot_1_address_and_wif(self) -> None:
        account = get_account(PASSWORD, 1)
        assert account.address() == "bc1q0l7cwnvtfwt538vv9yreuyrxuamgl470fkl8j0"
        assert account.wif() == "KykJVUYMpC2xCrUb8dENcRP3cvQHt6BQCskrkJvyW7fvGEVxpgWA"


class TestGetEmailAccount:
    def test_matches_known_email_password_derived_address_and_wif(self) -> None:
        account = get_email_account(EMAIL, PASSWORD)
        assert account.address() == "bc1qwjwpxtglzrv06ndfrqs2uegtmamg8qdzy3mghk"
        assert account.wif() == "L4m915x48Hasve5Dzc2BnJuUaxX85xWeF8PtUo5dP234VtSZjDtL"
