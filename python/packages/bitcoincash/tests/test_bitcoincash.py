import json
from pathlib import Path

import pytest

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

_CASES = _VECTORS["accounts"]["bitcoincash"]


@pytest.mark.parametrize("c", _CASES, ids=_ids(_CASES))
def test_matches_shared_vector(c: dict) -> None:
    account = _make(c, get_email_account, get_account)
    assert account.address() == c["address"]
    assert account.wif() == c["wif"]
