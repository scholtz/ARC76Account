import pytest

from arc76.core import derive_seed, derive_seed_with_email

PASSWORD = "12345678901234567890123456789012345678901234567890"
EMAIL = "email@example.com"


class TestDeriveSeed:
    def test_returns_deterministic_32_byte_seed(self) -> None:
        a = derive_seed(PASSWORD)
        b = derive_seed(PASSWORD)
        assert len(a) == 32
        assert a == b

    def test_differs_per_slot(self) -> None:
        slot0 = derive_seed(PASSWORD, 0)
        slot1 = derive_seed(PASSWORD, 1)
        assert slot0 != slot1

    def test_rejects_short_passwords(self) -> None:
        with pytest.raises(ValueError):
            derive_seed("short")

    def test_rejects_non_integer_slots(self) -> None:
        # Regression test - see audits/2026-08-04-audit-report.md (Low finding): a fractional
        # slot derives a seed the .NET package's `int slot` parameter cannot represent, and a
        # bool slot is almost certainly a caller bug, so both must be rejected rather than
        # silently accepted (Python's type hints are not enforced at runtime).
        with pytest.raises(TypeError):
            derive_seed(PASSWORD, 1.5)  # type: ignore[arg-type]
        with pytest.raises(TypeError):
            derive_seed(PASSWORD, True)  # type: ignore[arg-type]


class TestDeriveSeedWithEmail:
    def test_differs_from_password_only_seed(self) -> None:
        without_email = derive_seed(PASSWORD)
        with_email = derive_seed_with_email(EMAIL, PASSWORD)
        assert without_email != with_email

    def test_is_deterministic_for_same_email_password_slot(self) -> None:
        a = derive_seed_with_email(EMAIL, PASSWORD, 0)
        b = derive_seed_with_email(EMAIL, PASSWORD, 0)
        assert a == b
