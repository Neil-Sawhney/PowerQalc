# PowerQalc

A [PowerToys Command Palette](https://learn.microsoft.com/en-us/windows/powertoys/command-palette/extensibility-overview) extension that evaluates expressions using the [Qalculate](https://qalculate.github.io/) engine (`qalc`).

Type math, combine **mixed units in one expression**, convert the result, solve equations, and copy answers — without leaving the launcher.

Unlike a simple unit converter, PowerQalc lets you **do real math with incompatible units** and convert only when you want. Add miles to kilometers, multiply speed by time, combine feet and inches — then convert to whatever you need.

It also ships with a **huge library of built-in physical, chemical, and mathematical constants** — far beyond what a typical calculator offers. Use them by name in any expression, with automatic unit handling.

<img width="808" height="411" alt="image" src="https://github.com/user-attachments/assets/c15a3405-fec4-4481-a8b8-5589ea2fc27c" />


## Features

- Calculation history
- Copy results to the clipboard
- Variables and custom functions can be saved and used

## Example expressions

A few things to try — each shows a different capability:

| What it shows | Example | Result (approx.) |
|---------------|---------|------------------|
| **Mixed units** | `5 miles + 10 km` | `18 km` |
| **Session & variables** | `ans+1` · `B:=10 ft` then `B to cm` | Prior result; store as `B`, not `x` |
| **Solve for x, convert result** | `10 mph * x = 20 mi to min` | `x = 120 min` |
| **Percentages** | `240 * 15%` | `36` |
| **Algebra** | `x^2 + 2x = 0` | `x = 0 or x = -2` |
| **Named constants** | `planck to eV s` | Planck constant in eV·s |
| **Exotic units** | `1 ly to km` | ~9.5 trillion km |

Qalculate knows hundreds of constants (`FineStructure`, `proton_mass`, `SpeedOfLight`, …) and units from everyday measures to light years and electron-volts — use them in any expression.

## Requirements

- Windows 10/11 with [PowerToys](https://github.com/microsoft/PowerToys) (Command Palette enabled)

PowerQalc bundles the Qalculate CLI (`qalc`) — no separate Qalculate install needed.

## Installation

Install from the [Microsoft Store](https://apps.microsoft.com/detail/9MZR396NKKGW), then enable **Command Palette** in PowerToys settings. Open Command Palette and search for **PowerQalc**.

After installing or updating, run **Reload** in Command Palette if the extension does not appear right away.

For quicker access, assign a short alias in **PowerToys → Command Palette → Extensions → PowerQalc** — for example, `q` lets you open it with `q` then Space in Command Palette.

## License

MIT — see [LICENSE](LICENSE). Bundles the Qalculate CLI (`qalc`), licensed under GPL-2.0. License files ship in `qalc/licenses/` and `qalc/ThirdPartyNotices.txt` inside the MSIX.
