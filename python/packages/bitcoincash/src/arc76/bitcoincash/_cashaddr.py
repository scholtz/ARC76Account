"""Minimal CashAddr (P2PKH) encoder, following the reference algorithm from the CashAddr spec:
https://github.com/bitcoincashorg/bitcoincash.org/blob/master/spec/cashaddr.md

Not published as a general-purpose CashAddr library - kept internal and P2PKH-only (all this
package needs) rather than depending on the unmaintained `cashaddress` PyPI package. Verified
against known-answer values shared with dotnet/TestARC76/BitcoinCashTests.cs and
typescript/packages/bitcoincash/src/index.test.ts (see ../../tests/test_bitcoincash.py).
"""

from __future__ import annotations

_CHARSET = "qpzry9x8gf2tvdw0s3jn54khce6mua7l"
_GENERATOR = (0x98F2BC8E61, 0x79B76D99E2, 0xF33E5FB3C4, 0xAE2EABE2A8, 0x1E4F43E470)


def _polymod(values: list[int]) -> int:
    checksum = 1
    for value in values:
        top = checksum >> 35
        checksum = ((checksum & 0x07FFFFFFFF) << 5) ^ value
        for i in range(5):
            if (top >> i) & 1:
                checksum ^= _GENERATOR[i]
    return checksum ^ 1


def _prefix_expand(prefix: str) -> list[int]:
    return [ord(c) & 0x1F for c in prefix] + [0]


def _convert_bits(data: bytes, from_bits: int, to_bits: int) -> list[int]:
    acc = 0
    bits = 0
    result: list[int] = []
    max_value = (1 << to_bits) - 1
    max_acc = (1 << (from_bits + to_bits - 1)) - 1
    for value in data:
        acc = ((acc << from_bits) | value) & max_acc
        bits += from_bits
        while bits >= to_bits:
            bits -= to_bits
            result.append((acc >> bits) & max_value)
    if bits:
        result.append((acc << (to_bits - bits)) & max_value)
    return result


def encode_p2pkh(prefix: str, hash160: bytes) -> str:
    """Encodes a 20-byte P2PKH hash160 as a CashAddr, e.g. 'bitcoincash:q...'."""
    version_byte = 0  # P2PKH, 160-bit hash size
    payload = _convert_bits(bytes([version_byte]) + hash160, 8, 5)
    checksum_input = _prefix_expand(prefix) + payload + [0] * 8
    checksum = _polymod(checksum_input)
    checksum_digits = [(checksum >> (5 * (7 - i))) & 31 for i in range(8)]
    return prefix + ":" + "".join(_CHARSET[d] for d in payload + checksum_digits)
