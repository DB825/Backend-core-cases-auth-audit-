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

```bash
python3 -m pip install pillow
python3 demo/make_specimens.py     # writes demo/specimens/*.png
node demo/seed.mjs                 # loads all five into the API as analyst1, ready for a decision
```

Each seed run adds five new cases. To reset, stop the API and delete
`src/CaseAuth.Api/caseauth.db` and `src/CaseAuth.Api/fixture-uploads/`.
