"""Extract intentional task-closing references from a pull-request description."""

from __future__ import annotations

import re
import sys


def closing_numbers(body: str) -> list[int]:
    preamble = body.split("## ", 1)[0]
    return sorted({int(number) for number in re.findall(r"^Closes #([0-9]+)[ \t]*$", preamble, re.IGNORECASE | re.MULTILINE)})


if __name__ == "__main__":
    for number in closing_numbers(sys.stdin.read()):
        print(number)
