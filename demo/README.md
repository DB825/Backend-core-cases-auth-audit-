# Demo personas

Five synthetic applicants for the demo, one per scenario in the brief:

| Key | Applicant | What it shows | Expected outcome |
|---|---|---|---|
| `clean` | Maria Elena Torres | Everything agrees, sanctions screen clear | AI recommends approve |
| `mismatch` | John Smith | W-9 says "Smyth" and a Florida address; ID says "Smith", Illinois | Escalate, request proof of address |
| `expired` | Aisha Rahman | Passport expired 2024-06-30, DOB read at 58% confidence | Escalate, request a current ID |
| `sanctions` | Ruslan Tarkhovsky | 0.91 fuzzy match to a (synthetic) watchlist entry, DOB off by a year | Escalate to the sanctions officer |
| `shell` | Bluewater Meridian Holdings LLC | Nominee BVI owner, undisclosed 40%, PO box, address differs from registered address, phone shared with John Smith | AI recommends reject |

All data is invented. TINs use 987-65-432x, which the SSA reserves for advertising, and every
image is stamped SPECIMEN. The watchlist entry is made up, not a real SDN record.

`personas.json` is also the fixture for the other modules: Teammate 2 can check extraction
against `documents[].fields`, Teammate 3 can check its rules produce `findings`, and
Teammate 4 can compare its output with `aiReview`.

The quickest way to load them is the dashboard's **Reset demo** button (as `supervisor`), or
the API call it makes:

```bash
curl -X POST localhost:5020/api/demo/reset -H "X-Dev-User: supervisor"
```

That deletes the caller's firm's cases (their audit events stay, so the log is still
append-only) and loads all five personas straight into AwaitingDecision. It only exists in the
Development environment, and only a supervisor can call it. It reads `demo/personas.json` and
`demo/specimens/`, found by walking up from the API's content root, or from `Demo:DataPath`.

To load them over plain HTTP instead (for example against a deployed API):

```bash
python3 -m pip install pillow
python3 demo/make_specimens.py     # writes demo/specimens/*.png
node demo/seed.mjs                 # loads all five into the API as analyst1, ready for a decision
```

Each `seed.mjs` run adds five new cases rather than replacing them.

Finding codes match the Angular workbench's plain-language rules where one exists
(`NAME_MISMATCH`, `ADDRESS_MISMATCH`, `EXPIRED_ID`, `LOW_CONFIDENCE_EXTRACTION`).
