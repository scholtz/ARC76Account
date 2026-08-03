import base58

from arc76.solana import get_account, get_email_account

PASSWORD = "12345678901234567890123456789012345678901234567890"
EMAIL = "email@example.com"


# Known-answer values shared with dotnet/TestARC76/SolanaTests.cs and
# typescript/packages/solana/src/index.test.ts - if these ever diverge, cross-language ARC76
# compatibility has broken.
class TestGetAccount:
    def test_matches_known_password_derived_address_and_private_key(self) -> None:
        account = get_account(PASSWORD)
        assert base58.b58encode(account.public_key).decode() == (
            "4NZNrQAS9gLtqcutnCRfyy63M7XysEzgPP4hFGrutsne"
        )
        assert base58.b58encode(account.secret_key).decode() == (
            "3wvbZFR7njig8pAcSaHtzAVXYe779HjwrxCYiXRJS7shfWV6N3bNNynRijtz5WccRfDfhcHF5VTyq8yCv5HMAf34"
        )

    def test_matches_known_slot_1_address_and_private_key(self) -> None:
        account = get_account(PASSWORD, 1)
        assert base58.b58encode(account.public_key).decode() == (
            "D9cyFwbaGdDd1Lo3MJbL6VULN8r4p3c2jrWURTUV7kB"
        )
        assert base58.b58encode(account.secret_key).decode() == (
            "2WT1aN7vJse9uykpD6kgZs5zUovYKUxDKDEswPwEa9JnLgrEKseRQpxatFXfDHs9UohZB9A7qGRRZdBGzkWSi8K9"
        )


class TestGetEmailAccount:
    def test_matches_known_email_password_derived_address_and_private_key(self) -> None:
        account = get_email_account(EMAIL, PASSWORD)
        assert base58.b58encode(account.public_key).decode() == (
            "GcsDHS3b2hSUAWb2sU3UYvNWjudn4zas2h969q8C6P8r"
        )
        assert base58.b58encode(account.secret_key).decode() == (
            "5Vwqas2RYTc5iq4eN4cvx79AfZPY7ygvLt2AtQ5eMdzT9ZgeLDu57m2tpnyw7Z53TtZknyEs1qUMvfwhUkVeDLhC"
        )
