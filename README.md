# Hearthstone Bakery — SkyNet Bakery Template

A responsive bakery website with online ordering for **ASP.NET Core (.NET 10)**, built on the **SkyNet Framework**.

[![.NET 10](https://img.shields.io/badge/.NET-10-512BD4)](https://dotnet.microsoft.com/)
[![NuGet](https://img.shields.io/nuget/v/TheSkyLite.SkyNet?label=TheSkyLite.SkyNet)](https://www.nuget.org/packages/TheSkyLite.SkyNet)

> **Sample content.** Hearthstone Bakery, its 13 products, team and prices are invented placeholders; replace them in `data/site.json` or in the staff area. Every form already saves for real: orders go to `data/orders.json`, contact messages to `data/messages.json`, staff and the activity log to `data/staff.json` and `data/activity.json`.

## Features

- **15 pages:** Home, Menu, Order, Track, About, Contact (public), AdminLogin, Admin (dashboard), AdminOrders, AdminProducts, AdminMessages, AdminUsers, AdminSettings, AdminActivity, AdminAccount (staff).
- **Responsive:** desktop, tablet and phone layouts.
- **Menu:** the server filters by kind and searches; the browser keeps the basket (product keys and quantities only).
- **Online orders:** the server rebuilds the basket from current products, drops sold-out items, adds the delivery fee (free over a threshold), checks the minimum for delivery, the delivery ZIP codes, the open days, the baking notice and the orders-per-window limit, then saves the order with a number like `HB-1001`. Payment is on collection or delivery; no card details are taken.
- **Track:** customers look up an order with its number and email and see its status.
- **Staff area:** orders (status steps, cancel with reason), products (prices, sold out, add, delete), messages, users with a rights grid, settings (hours, fees, ZIPs, windows, closed dates), activity log, own account.
- **No front-end build:** plain HTML, CSS and vanilla JavaScript.

## Getting started

Requirements: .NET 10 SDK.

    cd Bakery
    dotnet run

The SkyNet package (`TheSkyLite.SkyNet`) restores from NuGet. The app opens on **Home** at http://localhost:5342.

Order times use the server clock, so set the server's time zone to the bakery's.

## Permissions

| Area | Rights | Extra right |
|---|---|---|
| Orders | View, Edit (move to baking, ready, done) | Cancel orders |
| Products | View, Edit | |
| Messages | View, Edit (mark read, delete) | |
| Activity log | View | |
| Users, Settings | managers only | |

A person without the Activity log right sees only **My activity**. Refused requests are written to the log as *Refused*.

## First sign-in

1. Run the site on your computer and open **/AdminLogin**. The page shows the first user name (`admin`) and a password made just for this site. It is shown only on the computer the site runs on, and is also in `appConfig/initial-admin.txt`.
2. Sign in and choose your own password. The box and the file disappear.
3. Open **Users** and add an account for each person; they choose their own password at first sign-in.

Forgot the first password before using it? Delete `appConfig/initial-admin.txt` and open the sign-in page again. Locked out of every manager account? Remove the managers from `data/staff.json` and open the sign-in page.

## Deploying

1. Publish with the `web.config` from the project (IIS). Give the application pool Modify on `data/`, `temp/` and `appConfig/`. Use HTTPS.
2. If nobody has signed in yet, read the first password from `appConfig/initial-admin.txt` on the server.
3. Check that `/data/staff.json` and `/appConfig/initial-admin.txt` answer 404 in a browser.

> **Redeploying:** publishing copies `data/` over the live files. Download the live `data/` folder first or leave it out of the publish.

## Project structure

    appConfig/application.cfg
    codes/            15 page classes, Core/ (sign-in, rights, storage), Models/
    htmls/ scripts/ styles/   one file per page
    data/site.json    brand, hours, order rules, products, team, FAQ
    images/           18 WebP images (hero, about, products/, team/)

### One page = four files, one name

| File | Holds |
|---|---|
| `codes/[Page].cs` | the page class and its server methods |
| `htmls/[Page].html` | markup with `{plhd_*}` placeholders |
| `scripts/[Page].js` | one IIFE namespace, `[Page]Js` |
| `styles/[Page].css` | styles, every class prefixed (`hm-`, `mn-`, `od-`, `tr-`, `ab-`, `ct-`, `lg-`, `db-`, `ao-`, `ap-`, `am-`, `au-`, `as-`, `ay-`, `aa-`) |

## SkyNet in action

```javascript
call('Quote', [{ key: 'cart', vlu: 'country-sourdough:2;seeded-rye:1' }, { key: 'mode', vlu: 'delivery' }, { key: 'date', vlu: '2026-10-21' }]);
```

```csharp
public async Task<ApiResponse> Quote()
{
    Basket basket = BuildBasket(site, GetDataValue("cart"));
    response.SetElementContents("od-sum", SummaryHtml(basket, mode, site.Rules));
}
```

| Page | Request | Response |
|---|---|---|
| Menu | `Filter` | new product grid, chips and count |
| Order | `Quote` | basket lines, totals, time windows |
| Order | `Place` | field errors, or the confirmation and a saved order |
| Track | `Find` | the order's status and items |
| Contact | `Send` | field errors, or a saved message |

## Customize it

- **Content, hours, fees:** `data/site.json`, or the Settings and Products pages.
- **Brand name, colors, fonts:** the `--xx-*` variables at the top of each page's CSS (`styles/*.css`); fonts use system stacks, so add a web font in every CSS file if you want one.
- **Product pictures:** add a `.webp` to `images/products/` and pick it in the product form.

## License

- **SkyNet Framework** (`TheSkyLite.SkyNet` NuGet package): proprietary, free to use including commercial use; see the license included in the package.
