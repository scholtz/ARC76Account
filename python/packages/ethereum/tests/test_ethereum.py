import json
from pathlib import Path

import pytest

from arc76.ethereum import get_account, get_email_account

PASSWORD = "12345678901234567890123456789012345678901234567890"
EMAIL = "email@example.com"


# Known-answer values shared with dotnet/TestARC76/EVMTests.cs and
# typescript/packages/ethereum/src/index.test.ts - if these ever diverge, cross-language ARC76
# compatibility has broken.
class TestGetAccount:
    def test_matches_known_password_derived_address_and_private_key(self) -> None:
        account = get_account(PASSWORD)
        assert account.address == "0x6fD4983EaC3F958d1BF2009be4FCc0E8792A8AC6"
        assert (
            account.key.hex()
            == "9365ff3d966fdb3b8765e27007677440e67382ed4503c2d00a39638548e13c22"
        )

    def test_matches_known_slot_1_address_and_private_key(self) -> None:
        account = get_account(PASSWORD, 1)
        assert account.address == "0x90a4b3436c28dBD111130683bacd4948B06A4544"
        assert (
            account.key.hex()
            == "4b698752e1f979bd51179faec7e59dcf7552d2724dd5a0b7a024d0ca536e4130"
        )


class TestGetEmailAccount:
    def test_matches_known_email_password_derived_address_and_private_key(self) -> None:
        account = get_email_account(EMAIL, PASSWORD)
        assert account.address == "0x0b6CEF2E7844F27019Fce92205F5FedF684c6926"
        assert (
            account.key.hex()
            == "e1078b9af5d2b58abc819135f6b8f358a3a36dd04d4707eb51f481d766a86915"
        )


# Extends the literal tests above with the cross-language vectors in test-vectors/arc76-vectors.json
# (repo root, shared with dotnet/ and typescript/) - if the languages diverge, cross-language
# ARC76 compatibility has broken.
_VECTORS = json.loads(
    (Path(__file__).resolve().parents[4] / "test-vectors" / "arc76-vectors.json").read_text(
        encoding="utf-8"
    )
)


def _ids(cases: list) -> list:
    return [f"{c['email']}-slot{c['slot']}-{c['password'][:8]}" for c in cases]


def _make(c: dict, with_email, without_email):
    if c["email"] is None:
        return without_email(c["password"], c["slot"])
    return with_email(c["email"], c["password"], c["slot"])

_CASES = _VECTORS["accounts"]["ethereum"]


@pytest.mark.parametrize("c", _CASES, ids=_ids(_CASES))
def test_matches_shared_vector(c: dict) -> None:
    account = _make(c, get_email_account, get_account)
    assert account.address == c["address"]
    assert account.key.hex() == c["privateKeyHex"]
