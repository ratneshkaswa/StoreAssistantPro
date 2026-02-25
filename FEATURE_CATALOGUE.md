# StoreAssistantPro — Complete Feature Catalogue (948 Features)

> **470 Core Features** (6 phases / 28 sprints) + **478 Expansion Features** (22 gates)

---

## PART A: CORE FEATURES (470)

### Phase 1: Product Foundation (Sprint 1–5) — 95 features

#### Sprint 1: Product Model Enrichment (Window: ProductsView) — 27 features

| # | Feature | Priority | Description |
|---|---------|----------|-------------|
| 1 | Product Name | P0 | Required text field, max 200 chars |
| 2 | Sale Price | P0 | Decimal price shown to customer |
| 3 | Quantity | P0 | Integer stock count |
| 4 | Tax Profile FK | P0 | Link product to a GST tax profile |
| 5 | HSN Code | P0 | 4-8 digit Harmonized System code for GST invoices |
| 6 | Tax Inclusive flag | P0 | Whether sale price includes or excludes tax |
| 7 | RowVersion (concurrency) | P0 | Optimistic concurrency via EF timestamp |
| 8 | Cost Price | P0 | Purchase/landing cost for margin calculation |
| 9 | MRP (Maximum Retail Price) | P0 | Legal max price printed on product (Indian regulation) |
| 10 | SKU / Item Code | P0 | Unique store-assigned product code |
| 11 | Barcode | P0 | EAN-13 / Code128 barcode string, unique index |
| 12 | Unit of Measurement | P1 | pcs, meters, sets, pairs, dozen — editable ComboBox |
| 13 | Minimum Stock Level | P1 | Reorder point; triggers low-stock alerts |
| 14 | Active/Inactive toggle | P1 | Hide inactive products from billing |
| 15 | Description / Notes | P2 | Free-text notes, max 500 chars |
| 16 | Product Image path | P2 | File path to product photo |
| 17 | Created / Modified date | P2 | Auto-set timestamps using IST |
| 18 | Duplicate / Clone product | P1 | Prefill Add form from selected product |
| 19 | Search by Barcode | P0 | Extend search to match barcode field |
| 20 | Search by SKU | P0 | Extend search to match SKU field |
| 21 | Filter by Category | P1 | Dropdown filter in toolbar (depends on #30) |
| 22 | Filter by Brand | P1 | Dropdown filter in toolbar (depends on #32) |
| 23 | Filter by Stock Status | P1 | All / In Stock / Low Stock / Out of Stock |
| 24 | Filter by Active/Inactive | P1 | All / Active Only / Inactive Only |
| 25 | Sale Price ≤ MRP enforcement | P1 | Validation rule preventing price above MRP |
| 26 | Bulk Import (CSV) | P1 | Parse CSV, bulk-insert products |
| 27 | Bulk Export (CSV) | P1 | Export all products to CSV file |

#### Sprint 2: Categories & Brands (Window: CategoriesView + BrandsView) — 20 features

| # | Feature | Priority | Description |
|---|---------|----------|-------------|
| 28 | Category model | P0 | Id, Name, Description, IsActive, ParentCategoryId |
| 29 | Category CRUD | P0 | Add / Edit / Delete categories |
| 30 | Product → Category FK | P0 | Link each product to one category |
| 31 | Category hierarchy (parent/child) | P2 | Self-referencing tree for subcategories |
| 32 | Brand model | P1 | Id, Name, LogoPath, IsActive |
| 33 | Brand CRUD | P1 | Add / Edit / Delete brands |
| 34 | Product → Brand FK | P1 | Link each product to one brand |
| 35 | Category search | P1 | Text search in category list |
| 36 | Category paging | P1 | Server-side paging for category list |
| 37 | Brand search | P1 | Text search in brand list |
| 38 | Brand paging | P1 | Server-side paging for brand list |
| 39 | Default category | P2 | Auto-assign new products to "Uncategorized" |
| 40 | Category product count | P2 | Show number of products per category |
| 41 | Brand product count | P2 | Show number of products per brand |
| 42 | Bulk assign category | P2 | Multi-select products → assign category |
| 43 | Bulk assign brand | P2 | Multi-select products → assign brand |
| 44 | Category import (CSV) | P2 | Bulk import categories |
| 45 | Category export (CSV) | P2 | Export categories to CSV |
| 46 | Brand import (CSV) | P2 | Bulk import brands |
| 47 | Brand export (CSV) | P2 | Export brands to CSV |

#### Sprint 3: Product Variants & Sizes (Window: ProductsView enrichment) — 18 features

| # | Feature | Priority | Description |
|---|---------|----------|-------------|
| 48 | Size model | P0 | Id, Name (S/M/L/XL/XXL/Free), SortOrder |
| 49 | Color model | P0 | Id, Name, HexCode |
| 50 | Product Variant model | P0 | ProductId + SizeId + ColorId + Barcode + Qty + AdditionalPrice |
| 51 | Variant CRUD | P0 | Add / Edit / Delete variants per product |
| 52 | Variant stock tracking | P0 | Each variant has its own quantity |
| 53 | Variant barcode (unique) | P0 | Unique barcode per variant |
| 54 | Variant price adjustment | P1 | ± price offset from base product price |
| 55 | Size master CRUD | P1 | Manage available sizes |
| 56 | Color master CRUD | P1 | Manage available colors |
| 57 | Size group templates | P2 | "Shirt sizes", "Trouser sizes" quick-fill |
| 58 | Variant grid view | P1 | Size × Color matrix showing stock |
| 59 | Variant search in billing | P0 | Scan variant barcode → add to cart |
| 60 | Variant low stock alert | P1 | Per-variant min stock check |
| 61 | Bulk variant creation | P1 | Select sizes + colors → auto-create all combos |
| 62 | Variant import (CSV) | P2 | Bulk import variants |
| 63 | Variant export (CSV) | P2 | Export variants to CSV |
| 64 | Variant image per color | P2 | Different photo per color variant |
| 65 | Variant inactive toggle | P2 | Deactivate specific size/color combos |

#### Sprint 4: Inventory Basics (Window: InventoryView — new) — 15 features

| # | Feature | Priority | Description |
|---|---------|----------|-------------|
| 66 | Stock adjustment (manual +/-) | P0 | Increase or decrease stock with reason |
| 67 | Adjustment reason codes | P0 | Damage, Theft, Correction, Return, Transfer |
| 68 | Stock adjustment log | P0 | Audit trail of all adjustments with timestamp/user |
| 69 | Stock take / physical count | P1 | Record physical count, auto-calculate difference |
| 70 | Low stock alert dashboard | P0 | List products where Qty ≤ MinStockLevel |
| 71 | Out of stock alert | P0 | List products where Qty = 0 |
| 72 | Stock value report | P1 | Total inventory value (Qty × CostPrice) |
| 73 | Stock movement history | P1 | Timeline of all stock changes per product |
| 74 | Opening stock entry | P1 | Set initial stock when onboarding |
| 75 | Stock freeze (period lock) | P2 | Lock stock edits during audit |
| 76 | Batch stock adjustment | P1 | Adjust multiple products at once |
| 77 | Stock adjustment approval | P2 | Manager PIN required for large adjustments |
| 78 | Inventory export (CSV) | P1 | Export current stock snapshot |
| 79 | Inventory import (CSV) | P1 | Bulk update stock from CSV |
| 80 | Dead stock report | P2 | Products with zero sales in N days |

#### Sprint 5: Supplier / Vendor Management (Window: SuppliersView — new) — 15 features

| # | Feature | Priority | Description |
|---|---------|----------|-------------|
| 81 | Supplier model | P0 | Id, Name, ContactPerson, Phone, Email, GSTIN, Address |
| 82 | Supplier CRUD | P0 | Add / Edit / Delete / Search / Page |
| 83 | Product → Supplier FK | P1 | Link product to primary supplier |
| 84 | Supplier active/inactive | P1 | Deactivate without deleting |
| 85 | Supplier search | P1 | Search by name, phone, GSTIN |
| 86 | Supplier GSTIN validation | P1 | 15-char Indian GSTIN format check |
| 87 | Supplier ledger | P2 | Running balance of purchases/payments |
| 88 | Supplier import (CSV) | P2 | Bulk import suppliers |
| 89 | Supplier export (CSV) | P2 | Export suppliers to CSV |
| 90 | Supplier payment tracking | P2 | Record payments made to supplier |
| 91 | Supplier due alerts | P2 | Alert when payment is overdue |
| 92 | Multiple suppliers per product | P2 | Many-to-many product-supplier mapping |
| 93 | Supplier price list | P2 | Cost price per supplier per product |
| 94 | Supplier notes | P2 | Free-text notes per supplier |
| 95 | Supplier product count | P2 | Show products supplied per vendor |

---

### Phase 2: Billing & Sales (Sprint 6–10) — 95 features

#### Sprint 6: Billing Core (Window: SalesView enrichment) — 20 features

| # | Feature | Priority | Description |
|---|---------|----------|-------------|
| 96 | Add product to cart by name search | P0 | Type-ahead search in billing |
| 97 | Add product by barcode scan | P0 | Barcode input → instant add |
| 98 | Cart line: quantity edit | P0 | Change qty per line item |
| 99 | Cart line: remove item | P0 | Delete line from cart |
| 100 | Cart line: price override | P1 | Change sale price (manager PIN) |
| 101 | Cart subtotal display | P0 | Sum of all line totals |
| 102 | Cart tax display | P0 | Total tax (CGST + SGST or IGST) |
| 103 | Cart grand total | P0 | Subtotal + Tax − Discount |
| 104 | Bill-level discount (₹) | P0 | Flat rupee discount on bill |
| 105 | Bill-level discount (%) | P0 | Percentage discount on bill |
| 106 | Line-level discount (₹) | P1 | Flat discount per line item |
| 107 | Line-level discount (%) | P1 | Percentage discount per line item |
| 108 | Payment: Cash | P0 | Accept cash payment |
| 109 | Payment: Cash + change calculation | P0 | Auto-calculate change due |
| 110 | Complete sale → deduct stock | P0 | Atomic: save sale + reduce product qty |
| 111 | Sale number auto-generation | P0 | Sequential invoice number |
| 112 | Sale timestamp (IST) | P0 | Record sale date/time in IST |
| 113 | Sale → User tracking | P0 | Which user made the sale |
| 114 | Cart clear / cancel bill | P0 | Discard entire cart |
| 115 | Billing keyboard shortcuts | P0 | F2=New, F5=Pay, Esc=Cancel, etc. |

#### Sprint 7: Payment Methods & Receipts (Window: SalesView + ReceiptView) — 20 features

| # | Feature | Priority | Description |
|---|---------|----------|-------------|
| 116 | Payment: UPI | P0 | Record UPI payment reference |
| 117 | Payment: Card (credit/debit) | P0 | Record card last-4 digits |
| 118 | Payment: Split payment | P1 | Part cash + part UPI/card |
| 119 | Payment: Credit (on account) | P1 | Sale on customer credit |
| 120 | Receipt generation (thermal 80mm) | P0 | Format receipt for thermal printer |
| 121 | Receipt: firm name + address | P0 | Print store details on receipt |
| 122 | Receipt: GSTIN on receipt | P0 | Print store GSTIN |
| 123 | Receipt: line items with tax | P0 | Print each item with price/qty/tax |
| 124 | Receipt: total / discount / tax summary | P0 | Print bill summary |
| 125 | Receipt: payment method | P0 | Print how customer paid |
| 126 | Receipt: thank you message | P1 | Configurable footer text |
| 127 | Receipt preview before print | P1 | On-screen preview |
| 128 | Receipt reprint | P1 | Reprint past receipt from sale history |
| 129 | Receipt: QR code | P2 | QR code with sale ID or UPI link |
| 130 | Receipt: barcode | P2 | Barcode of invoice number |
| 131 | A4 invoice format | P1 | Full-page tax invoice for B2B |
| 132 | Invoice: HSN-wise tax summary | P0 | GST-compliant HSN summary table |
| 133 | Invoice: CGST/SGST/IGST split | P0 | Show tax components separately |
| 134 | Invoice: customer details | P1 | Print buyer name/address/GSTIN |
| 135 | Email invoice (PDF) | P2 | Send invoice as PDF attachment |

#### Sprint 8: Sale History & Returns (Window: SalesView enrichment + ReturnsView) — 18 features

| # | Feature | Priority | Description |
|---|---------|----------|-------------|
| 136 | Sale history list | P0 | Paged list of all past sales |
| 137 | Sale history search by date | P0 | Filter sales by date range |
| 138 | Sale history search by invoice # | P0 | Find sale by invoice number |
| 139 | Sale detail view | P0 | View all line items of a past sale |
| 140 | Sale history export (CSV) | P1 | Export sales data |
| 141 | Sale return (full) | P0 | Return entire sale → restore stock |
| 142 | Sale return (partial) | P0 | Return specific items from a sale |
| 143 | Return reason code | P1 | Defective, Wrong size, Customer changed mind |
| 144 | Return → stock restoration | P0 | Auto-add returned qty back to product |
| 145 | Return → refund amount calc | P0 | Calculate refund based on returned items |
| 146 | Credit note generation | P1 | Issue credit note for returns |
| 147 | Exchange (return + new sale) | P1 | Return items and bill new items in one flow |
| 148 | Return approval (Manager PIN) | P1 | Require manager authorization for returns |
| 149 | Return history list | P1 | View all past returns |
| 150 | Return receipt print | P1 | Print return/credit note receipt |
| 151 | Daily return summary | P2 | Total returns per day |
| 152 | Return against original invoice | P0 | Link return to original sale ID |
| 153 | No-bill return (manager only) | P2 | Return without original invoice |

#### Sprint 9: Customer Management (Window: CustomersView — new) — 20 features

| # | Feature | Priority | Description |
|---|---------|----------|-------------|
| 154 | Customer model | P0 | Id, Name, Phone, Email, Address, GSTIN, CreatedDate |
| 155 | Customer CRUD | P0 | Add / Edit / Delete / Search / Page |
| 156 | Customer search by phone | P0 | Quick lookup by mobile number |
| 157 | Customer → Sale linking | P0 | Attach customer to a sale |
| 158 | Walk-in customer (default) | P0 | Sales without named customer |
| 159 | Customer purchase history | P1 | View all purchases by a customer |
| 160 | Customer outstanding balance | P1 | Total credit/unpaid amount |
| 161 | Customer payment collection | P1 | Record payment against outstanding |
| 162 | Customer loyalty points | P2 | Earn points on purchase, redeem on next |
| 163 | Customer tier (Regular/Silver/Gold) | P2 | Auto-tier based on spend |
| 164 | Customer birthday tracking | P2 | Date field for promotions |
| 165 | Customer anniversary tracking | P2 | Date field for promotions |
| 166 | Customer notes | P2 | Free-text notes per customer |
| 167 | Customer import (CSV) | P2 | Bulk import customers |
| 168 | Customer export (CSV) | P2 | Export customer list |
| 169 | Customer duplicate detection | P1 | Warn on same phone number |
| 170 | Customer active/inactive | P1 | Deactivate without deleting |
| 171 | Customer GSTIN validation | P1 | 15-char format check for B2B |
| 172 | Customer group/tag | P2 | Group customers for targeting |
| 173 | Customer credit limit | P2 | Max outstanding balance allowed |

#### Sprint 10: Discount & Promotion Engine (Window: DiscountsView — new) — 17 features

| # | Feature | Priority | Description |
|---|---------|----------|-------------|
| 174 | Flat discount on bill | P0 | ₹ off total bill |
| 175 | Percentage discount on bill | P0 | % off total bill |
| 176 | Line-item flat discount | P0 | ₹ off specific item |
| 177 | Line-item percentage discount | P0 | % off specific item |
| 178 | Discount approval (PIN) | P1 | Manager PIN for discounts above threshold |
| 179 | Maximum discount limit | P1 | Cap discount at configurable % |
| 180 | Buy X Get Y free | P2 | Promotional combo deal |
| 181 | Combo / bundle pricing | P2 | Special price for product set |
| 182 | Seasonal sale discount | P2 | Time-bound automatic discount |
| 183 | Category-wide discount | P2 | % off all products in category |
| 184 | Brand-wide discount | P2 | % off all products of a brand |
| 185 | Customer-specific discount | P2 | Special price for loyal customers |
| 186 | Coupon code support | P2 | Enter code → apply discount |
| 187 | Discount history log | P1 | Audit trail of all discounts given |
| 188 | Discount report (daily) | P1 | Total discounts given per day |
| 189 | Minimum bill for discount | P2 | Discount only if bill > threshold |
| 190 | Discount stacking rules | P2 | Control whether discounts combine |

---

### Phase 3: Financial & Tax (Sprint 11–15) — 80 features

#### Sprint 11: GST Tax Engine (Window: TaxManagementWindow enrichment) — 18 features

| # | Feature | Priority | Description |
|---|---------|----------|-------------|
| 191 | Tax Profile CRUD | P0 | Create tax rate profiles (5%, 12%, 18%, 28%) |
| 192 | CGST + SGST split | P0 | Intra-state tax: half CGST + half SGST |
| 193 | IGST for inter-state | P0 | Full rate as IGST for inter-state sales |
| 194 | Tax-inclusive price back-calc | P0 | Extract tax from inclusive price |
| 195 | Tax-exclusive price add-on | P0 | Add tax on top of base price |
| 196 | HSN-wise tax summary on invoice | P0 | Group tax by HSN code |
| 197 | Cess support | P1 | Additional cess on specific goods |
| 198 | Tax exemption flag | P1 | Mark product as tax-exempt |
| 199 | Composition scheme support | P2 | Flat rate for small businesses |
| 200 | Reverse charge mechanism | P2 | Buyer pays tax directly to govt |
| 201 | Tax profile active/inactive | P1 | Deactivate obsolete tax rates |
| 202 | Default tax profile | P1 | Auto-assign to new products |
| 203 | Tax rate change audit log | P1 | Track when rates were changed |
| 204 | Multi-tax-profile per product | P2 | Different rates for different states |
| 205 | Tax report (monthly) | P1 | Total tax collected per month |
| 206 | Tax report by HSN | P1 | Tax grouped by HSN code |
| 207 | GSTR-1 data export | P2 | Export data for GST return filing |
| 208 | GSTR-3B summary | P2 | Generate summary for monthly return |

#### Sprint 12: Purchase Orders (Window: PurchaseOrdersView — new) — 16 features

| # | Feature | Priority | Description |
|---|---------|----------|-------------|
| 209 | Purchase Order model | P1 | PO#, SupplierId, Date, Status, Items |
| 210 | PO creation | P1 | Create PO with line items |
| 211 | PO → Supplier linking | P1 | Each PO linked to a supplier |
| 212 | PO line items | P1 | Product, Qty ordered, Unit cost |
| 213 | PO status workflow | P1 | Draft → Sent → Partial → Received → Closed |
| 214 | PO receive goods | P1 | Mark items received → update stock |
| 215 | PO partial receipt | P1 | Receive some items, rest pending |
| 216 | PO cost price update | P1 | Update product cost from PO |
| 217 | PO search / filter | P1 | Search by PO#, supplier, date |
| 218 | PO paging | P1 | Server-side paging |
| 219 | PO print | P2 | Print purchase order for supplier |
| 220 | PO history | P1 | View past purchase orders |
| 221 | PO duplicate / reorder | P2 | Clone previous PO |
| 222 | PO auto-generate from low stock | P2 | Suggest PO for products below min stock |
| 223 | PO import (CSV) | P2 | Bulk create PO from file |
| 224 | PO export (CSV) | P2 | Export POs to file |

#### Sprint 13: Expense Tracking (Window: ExpensesView — new) — 14 features

| # | Feature | Priority | Description |
|---|---------|----------|-------------|
| 225 | Expense model | P1 | Id, Date, Category, Amount, PaymentMethod, Notes |
| 226 | Expense CRUD | P1 | Add / Edit / Delete expenses |
| 227 | Expense categories | P1 | Rent, Utilities, Salary, Transport, Misc |
| 228 | Expense category CRUD | P1 | Manage expense categories |
| 229 | Expense search by date | P1 | Date range filter |
| 230 | Expense search by category | P1 | Filter by expense type |
| 231 | Daily expense summary | P1 | Total expenses per day |
| 232 | Monthly expense report | P1 | Total expenses per month |
| 233 | Expense receipt photo | P2 | Attach photo of receipt |
| 234 | Recurring expense | P2 | Auto-create monthly expenses (rent) |
| 235 | Expense approval | P2 | Manager approval for large expenses |
| 236 | Expense paging | P1 | Server-side paging |
| 237 | Expense import (CSV) | P2 | Bulk import expenses |
| 238 | Expense export (CSV) | P2 | Export expenses to CSV |

#### Sprint 14: Cash Register / Day End (Window: CashRegisterView — new) — 16 features

| # | Feature | Priority | Description |
|---|---------|----------|-------------|
| 239 | Opening cash balance | P0 | Record cash in drawer at start of day |
| 240 | Cash in / cash out entries | P0 | Record manual cash additions/removals |
| 241 | Cash in/out reason | P0 | Reason code for each entry |
| 242 | Expected cash balance | P0 | Opening + Sales(cash) − Returns − CashOut + CashIn |
| 243 | Actual cash count (day end) | P0 | User enters physical count |
| 244 | Cash variance | P0 | Difference: actual − expected |
| 245 | Day end report | P0 | Summary of day's transactions |
| 246 | Day close / lockdown | P1 | Close day, prevent further edits |
| 247 | Day-wise cash history | P1 | View past day summaries |
| 248 | Payment method breakdown | P1 | Cash / UPI / Card totals for the day |
| 249 | Denomination entry | P2 | Count notes: ₹500×__, ₹200×__, etc. |
| 250 | Shift support | P2 | Multiple shifts per day per user |
| 251 | Cash register export | P2 | Export day summary to CSV |
| 252 | Cash variance alert | P1 | Warn if variance exceeds threshold |
| 253 | Cash register approval | P2 | Manager sign-off on day close |
| 254 | Cash register audit log | P1 | Full history of cash movements |

#### Sprint 15: Profit & Loss Basics (Window: ReportsView — new) — 16 features

| # | Feature | Priority | Description |
|---|---------|----------|-------------|
| 255 | Daily sales summary | P0 | Total sales, returns, net for the day |
| 256 | Monthly sales summary | P0 | Aggregate by month |
| 257 | Product-wise sales report | P0 | Qty sold + revenue per product |
| 258 | Category-wise sales report | P1 | Revenue per category |
| 259 | Brand-wise sales report | P1 | Revenue per brand |
| 260 | Profit margin per product | P1 | (Sale − Cost) per product |
| 261 | Gross profit report | P1 | Total revenue − total cost |
| 262 | Net profit (sales − expenses) | P1 | Gross profit − expenses |
| 263 | Best selling products | P1 | Top N by qty or revenue |
| 264 | Slow moving products | P1 | Bottom N by sales velocity |
| 265 | Sales by user report | P1 | Revenue per salesperson |
| 266 | Sales by payment method | P1 | Cash vs UPI vs Card breakdown |
| 267 | Sales by hour of day | P2 | Peak hour analysis |
| 268 | Customer-wise sales report | P2 | Revenue per customer |
| 269 | Report date range picker | P0 | Custom date range for all reports |
| 270 | Report export (CSV) | P1 | Export any report to CSV |

---

### Phase 4: Users & Security (Sprint 16–19) — 65 features

#### Sprint 16: User Role Enrichment (Window: UserManagementWindow enrichment) — 18 features

| # | Feature | Priority | Description |
|---|---------|----------|-------------|
| 271 | Admin role | P0 | Full access to all features |
| 272 | Manager role | P0 | All except system settings |
| 273 | User/Cashier role | P0 | Billing only, no management |
| 274 | Role-based menu visibility | P0 | Hide menu items per role |
| 275 | Role-based button visibility | P0 | Hide action buttons per role |
| 276 | PIN change (self) | P0 | User changes own PIN |
| 277 | PIN reset (admin) | P0 | Admin resets another user's PIN |
| 278 | User activity log | P1 | Login/logout timestamps |
| 279 | User session tracking | P1 | Current active sessions |
| 280 | User sales performance | P1 | Sales totals per user |
| 281 | User profile (name, photo) | P2 | Basic profile info |
| 282 | Multiple users per role | P0 | Multiple cashiers, managers |
| 283 | User active/inactive | P1 | Deactivate without deleting |
| 284 | User creation (admin only) | P0 | Admin creates new users |
| 285 | User deletion (admin only) | P0 | Soft-delete or deactivate |
| 286 | User search | P1 | Search by name |
| 287 | User import (CSV) | P2 | Bulk create users |
| 288 | User export (CSV) | P2 | Export user list |

#### Sprint 17: Permissions & Audit (Window: SecuritySettingsView enrichment) — 16 features

| # | Feature | Priority | Description |
|---|---------|----------|-------------|
| 289 | Permission matrix (role × feature) | P1 | Configurable permissions per role |
| 290 | Master PIN for destructive ops | P0 | Delete product/sale/user requires master PIN |
| 291 | Audit log model | P1 | Who did what, when, from where |
| 292 | Audit: product changes | P1 | Log product CRUD |
| 293 | Audit: sale completion | P0 | Log every completed sale |
| 294 | Audit: price override | P0 | Log any price change during billing |
| 295 | Audit: discount given | P0 | Log discounts with amount and reason |
| 296 | Audit: return processed | P1 | Log returns with reason |
| 297 | Audit: user login/logout | P1 | Login tracking |
| 298 | Audit: settings changed | P1 | Track config changes |
| 299 | Audit log viewer | P1 | Searchable, paged audit log UI |
| 300 | Audit log export | P2 | Export audit log to CSV |
| 301 | Failed login tracking | P1 | Count failed PIN attempts |
| 302 | Account lockout | P2 | Lock after N failed attempts |
| 303 | Session timeout | P2 | Auto-logout after inactivity |
| 304 | PIN complexity rules | P2 | Prevent weak PINs (1234, 0000) |

#### Sprint 18: Firm / Store Settings (Window: FirmManagementWindow enrichment) — 16 features

| # | Feature | Priority | Description |
|---|---------|----------|-------------|
| 305 | Firm name | P0 | Store name on receipts/invoices |
| 306 | Firm address | P0 | Full address |
| 307 | Firm phone | P0 | Contact number |
| 308 | Firm GSTIN | P0 | GST identification number |
| 309 | Firm PAN | P1 | PAN card number |
| 310 | Firm email | P1 | Contact email |
| 311 | Firm logo | P1 | Logo for receipts/invoices |
| 312 | Firm bank details | P2 | Bank name, account, IFSC |
| 313 | Invoice prefix config | P1 | Custom invoice number prefix |
| 314 | Invoice numbering reset | P2 | Reset counter annually/monthly |
| 315 | Receipt footer text | P1 | Custom message on receipts |
| 316 | Receipt header text | P2 | Additional header info |
| 317 | Currency symbol config | P1 | ₹ (Indian Rupee default) |
| 318 | Financial year start | P1 | April 1 (Indian FY) |
| 319 | Decimal places config | P2 | 0 or 2 decimal places |
| 320 | Store hours | P2 | Opening/closing time |

#### Sprint 19: Backup & Data Safety (Window: BackupSettingsView enrichment) — 15 features

| # | Feature | Priority | Description |
|---|---------|----------|-------------|
| 321 | Manual database backup | P0 | One-click SQLite backup to file |
| 322 | Auto backup on day close | P0 | Automatic backup at end of day |
| 323 | Backup location config | P0 | Choose backup folder |
| 324 | Backup history list | P1 | View past backups |
| 325 | Restore from backup | P0 | Restore database from backup file |
| 326 | Backup to USB | P1 | Direct backup to USB drive |
| 327 | Scheduled backup (hourly) | P2 | Background periodic backup |
| 328 | Backup encryption | P2 | Password-protect backup files |
| 329 | Cloud backup (OneDrive/GDrive) | P2 | Upload backup to cloud |
| 330 | Backup size tracking | P2 | Show backup file sizes |
| 331 | Old backup cleanup | P2 | Auto-delete backups older than N days |
| 332 | Backup notification | P1 | Remind if no backup in 24h |
| 333 | Data export (full) | P2 | Export all data as JSON/CSV |
| 334 | Data import (migration) | P2 | Import from another store instance |
| 335 | Database integrity check | P2 | Verify DB isn't corrupted |

---

### Phase 5: Advanced Billing & Operations (Sprint 20–24) — 75 features

#### Sprint 20: Hold & Recall Bills (Window: SalesView enrichment) — 12 features

| # | Feature | Priority | Description |
|---|---------|----------|-------------|
| 336 | Hold current bill | P0 | Park active cart for later |
| 337 | Recall held bill | P0 | Resume a parked cart |
| 338 | Multiple held bills | P0 | Hold up to N bills simultaneously |
| 339 | Held bill timeout | P2 | Auto-discard after configurable time |
| 340 | Held bill indicator | P1 | Badge showing count of held bills |
| 341 | Held bill customer tag | P1 | Attach customer name to held bill |
| 342 | Held bill notes | P2 | Add note to parked bill |
| 343 | Held bill history | P2 | Log of all hold/recall actions |
| 344 | Quick switch between held bills | P1 | Keyboard shortcut to cycle |
| 345 | Held bill auto-save | P0 | Persist held bills to DB |
| 346 | Held bill stale cleanup | P1 | Clean abandoned held bills on startup |
| 347 | Held bill limit per user | P2 | Max held bills per cashier |

#### Sprint 21: Quotation / Estimate (Window: QuotationsView — new) — 14 features

| # | Feature | Priority | Description |
|---|---------|----------|-------------|
| 348 | Quotation model | P1 | Id, Customer, Date, Items, Total, Status, ValidUntil |
| 349 | Quotation creation | P1 | Create quote with line items |
| 350 | Quotation print | P1 | Print formatted quotation |
| 351 | Quotation → Sale conversion | P1 | Convert accepted quote to sale |
| 352 | Quotation status | P1 | Draft → Sent → Accepted → Expired → Rejected |
| 353 | Quotation search | P1 | Search by customer, date, number |
| 354 | Quotation paging | P1 | Server-side paging |
| 355 | Quotation validity period | P1 | Auto-expire after N days |
| 356 | Quotation revision | P2 | Create revised version |
| 357 | Quotation duplicate | P2 | Clone existing quotation |
| 358 | Quotation history | P1 | View past quotations |
| 359 | Quotation export (CSV) | P2 | Export quotations |
| 360 | Quotation email | P2 | Email quotation PDF to customer |
| 361 | Quotation terms & conditions | P2 | Configurable T&C text |

#### Sprint 22: Purchase Receive & GRN (Window: GRNView — new) — 14 features

| # | Feature | Priority | Description |
|---|---------|----------|-------------|
| 362 | Goods Received Note model | P1 | GRN#, PO#, Date, Items, Supplier |
| 363 | GRN creation from PO | P1 | Receive against purchase order |
| 364 | GRN without PO | P2 | Direct receive (walk-in purchase) |
| 365 | GRN line items | P1 | Product, qty received, qty rejected |
| 366 | GRN → stock update | P1 | Auto-increase stock on confirm |
| 367 | GRN → cost price update | P1 | Update product cost from GRN |
| 368 | GRN quality check | P2 | Accept/reject per item |
| 369 | GRN search | P1 | Search by GRN#, PO#, supplier |
| 370 | GRN paging | P1 | Server-side paging |
| 371 | GRN print | P2 | Print GRN document |
| 372 | GRN history | P1 | View past GRNs |
| 373 | GRN export (CSV) | P2 | Export GRNs |
| 374 | Purchase return to supplier | P2 | Return defective goods to supplier |
| 375 | Debit note generation | P2 | Debit note for supplier returns |

#### Sprint 23: Barcode Operations (Window: BarcodeView — new) — 12 features

| # | Feature | Priority | Description |
|---|---------|----------|-------------|
| 376 | Barcode label printing | P1 | Print barcode labels for products |
| 377 | Barcode label template | P1 | Configurable label layout |
| 378 | Barcode: product name on label | P1 | Print name below barcode |
| 379 | Barcode: price on label | P1 | Print sale price on label |
| 380 | Barcode: MRP on label | P1 | Print MRP on label |
| 381 | Barcode: size/color on label | P2 | Print variant info |
| 382 | Batch barcode printing | P1 | Print N labels at once |
| 383 | Barcode scanner input handling | P0 | Detect scanner input vs keyboard |
| 384 | Barcode auto-generate | P2 | Generate barcode if not assigned |
| 385 | Barcode format selection | P2 | EAN-13 / Code128 / Code39 |
| 386 | Barcode label paper size | P2 | Configure label sheet dimensions |
| 387 | Barcode lookup (scan → show product) | P1 | Quick product info via scan |

#### Sprint 24: Dashboard & Analytics (Window: DashboardView enrichment) — 23 features

| # | Feature | Priority | Description |
|---|---------|----------|-------------|
| 388 | Today's sales total | P0 | Real-time daily sales |
| 389 | Today's return total | P0 | Daily returns |
| 390 | Today's net sales | P0 | Sales − Returns |
| 391 | Today's profit | P1 | Net sales − COGS |
| 392 | Today's transaction count | P0 | Number of bills |
| 393 | Average bill value | P1 | Total / count |
| 394 | Top 5 products today | P1 | Best sellers widget |
| 395 | Low stock alerts count | P0 | Badge on dashboard |
| 396 | Out of stock count | P0 | Badge on dashboard |
| 397 | Pending payments count | P1 | Customer credit outstanding |
| 398 | Monthly sales trend | P1 | Bar/line chart for 30 days |
| 399 | Monthly expense trend | P2 | Bar chart for expenses |
| 400 | Category-wise sales pie | P2 | Pie chart by category |
| 401 | Payment method pie | P1 | Cash/UPI/Card split |
| 402 | Year-over-year comparison | P2 | Compare to same month last year |
| 403 | Sales target vs actual | P2 | Configurable monthly target |
| 404 | Quick action tiles | P0 | New Sale, Add Product, View Reports |
| 405 | Recent sales list | P1 | Last 10 transactions |
| 406 | Upcoming tasks | P2 | Pending POs, overdue payments |
| 407 | Dashboard auto-refresh | P1 | Refresh every 60 seconds |
| 408 | Dashboard widget layout | P2 | Configurable widget positions |
| 409 | Dashboard print | P2 | Print daily summary |
| 410 | Dashboard date selector | P1 | View dashboard for past dates |

---

### Phase 6: Polish & UX (Sprint 25–28) — 60 features

#### Sprint 25: Keyboard Navigation & Accessibility — 15 features

| # | Feature | Priority | Description |
|---|---------|----------|-------------|
| 411 | Full keyboard navigation in billing | P0 | Tab through all fields |
| 412 | F-key shortcuts for all actions | P0 | F1=Help, F2=New, etc. |
| 413 | Ctrl+key shortcuts | P0 | Ctrl+N, Ctrl+S, Ctrl+P |
| 414 | Shortcut cheat sheet overlay | P1 | F1 shows all shortcuts |
| 415 | Focus visible indicator | P0 | Clear visual focus ring |
| 416 | Tab order optimization | P0 | Logical tab sequence |
| 417 | Escape to cancel/close | P0 | Escape key on all forms/dialogs |
| 418 | Enter to confirm/save | P0 | Enter key submits active form |
| 419 | Arrow keys in DataGrid | P0 | Navigate rows with arrows |
| 420 | Quick search focus (Ctrl+F) | P1 | Focus search box |
| 421 | Screen reader labels | P2 | AutomationProperties on controls |
| 422 | High contrast mode | P2 | Alternative color theme |
| 423 | Font size scaling | P2 | Configurable font scale |
| 424 | Keyboard-only billing flow | P0 | Complete a sale without mouse |
| 425 | Input focus restore | P1 | Restore focus after dialog close |

#### Sprint 26: Error Handling & Validation — 14 features

| # | Feature | Priority | Description |
|---|---------|----------|-------------|
| 426 | Required field validation | P0 | Red border + message for empty required fields |
| 427 | Numeric field validation | P0 | Prevent non-numeric in price/qty |
| 428 | Max length enforcement | P0 | Truncate/warn at max length |
| 429 | Duplicate barcode prevention | P0 | Unique constraint error → friendly message |
| 430 | Concurrency conflict handling | P0 | Reload + retry on optimistic lock failure |
| 431 | Network error graceful handling | P0 | Offline mode → queued operations |
| 432 | Validation summary panel | P1 | Show all errors at once |
| 433 | Field-level error tooltips | P1 | Tooltip on invalid field |
| 434 | Auto-retry on transient DB errors | P1 | EF Core retry strategy |
| 435 | Friendly error messages | P0 | No stack traces shown to user |
| 436 | Error logging to file | P0 | All exceptions logged |
| 437 | Unhandled exception handler | P0 | Global handler prevents crash |
| 438 | Confirmation dialogs | P0 | Confirm before delete/discard |
| 439 | Undo last action | P2 | Undo last delete (within session) |

#### Sprint 27: Print & Report Templates — 16 features

| # | Feature | Priority | Description |
|---|---------|----------|-------------|
| 440 | Thermal receipt (58mm) | P1 | Narrow thermal printer format |
| 441 | Thermal receipt (80mm) | P0 | Standard thermal printer format |
| 442 | A4 tax invoice | P1 | Full-page GST invoice |
| 443 | A5 half-page invoice | P2 | Compact invoice |
| 444 | Delivery challan print | P2 | Goods dispatch document |
| 445 | Barcode label sheet | P1 | Multiple labels per A4 sheet |
| 446 | Price tag printing | P2 | Product name + price labels |
| 447 | Daily sales report print | P1 | Printable daily summary |
| 448 | Monthly sales report print | P1 | Printable monthly summary |
| 449 | Stock report print | P1 | Current inventory printout |
| 450 | Customer statement print | P2 | Outstanding balance letter |
| 451 | Purchase order print | P2 | Printable PO |
| 452 | Quotation print | P2 | Printable estimate |
| 453 | Credit note print | P2 | Return credit document |
| 454 | Print preview for all | P1 | Preview before every print |
| 455 | Print settings (printer selection) | P1 | Choose default printer |

#### Sprint 28: System Settings & Config — 15 features

| # | Feature | Priority | Description |
|---|---------|----------|-------------|
| 456 | App version display | P0 | About screen with version info |
| 457 | Theme: Light/Dark | P2 | Color theme switching |
| 458 | Language: English (default) | P0 | English UI |
| 459 | Date format config | P1 | dd/MM/yyyy (Indian default) |
| 460 | Currency format config | P1 | ₹1,23,456.00 (Indian lakh format) |
| 461 | Page size config | P2 | 25/50/100 rows per page |
| 462 | Auto-logout timer | P2 | Configurable inactivity timeout |
| 463 | Sound effects toggle | P2 | Beep on scan, alert sounds |
| 464 | Startup mode config | P2 | Start in Billing or Management mode |
| 465 | License key / activation | P2 | License management |
| 466 | Update check | P2 | Check for new versions |
| 467 | System health monitor | P2 | DB size, memory usage |
| 468 | Reset to factory defaults | P2 | Clear all data (master PIN required) |
| 469 | Feature flags management | P1 | Enable/disable features |
| 470 | Onboarding wizard (first run) | P0 | First-time setup flow |

---

## PART B: EXPANSION FEATURES (478)

### G1: Hardware Integration (48 features)

| # | Feature | Description |
|---|---------|-------------|
| 471–478 | Barcode scanner integration | USB/serial scanner detection, continuous scan mode, scan-to-field routing, multi-scanner support, scan sound feedback, scan error handling, scanner config UI, wireless scanner |
| 479–486 | Thermal printer integration | Auto-detect printer, ESC/POS commands, logo printing, barcode on receipt, cash drawer kick, paper cut command, printer status, dual printer support |
| 487–494 | Cash drawer | Auto-open on sale, manual open (manager PIN), drawer status, multiple drawers, drawer assignment per register, open count tracking, drawer close alert, USB/serial support |
| 495–502 | Weighing scale | USB scale integration, auto-read weight, weight-to-qty mapping, tare support, scale config, multi-scale support, weight display, weight-based pricing |
| 503–510 | Label printer | Dedicated label printer, Zebra/TSC support, label templates, auto-feed, batch print, preview, printer config, wireless label printing |
| 511–518 | Customer display | Pole display support, LCD display, item-by-item display, total display, idle message, customer-facing screen, dual monitor, promotional content |

### G2: AI & Smart Features (22 features)

| # | Feature | Description |
|---|---------|-------------|
| 519–522 | Demand forecasting | Sales trend prediction, seasonal demand, reorder suggestions, stock optimization |
| 523–526 | Smart pricing | Competitor price tracking, dynamic pricing rules, markdown optimization, price elasticity |
| 527–530 | Customer insights | Purchase pattern analysis, churn prediction, segment auto-detection, next-purchase prediction |
| 531–534 | Smart search | Fuzzy search, phonetic matching, recent/frequent suggestions, barcode OCR from camera |
| 535–540 | Anomaly detection | Unusual transaction alerts, theft detection patterns, inventory shrinkage alerts, price anomaly, discount abuse detection, void pattern detection |

### G3: Commercial / Licensing (27 features)

| # | Feature | Description |
|---|---------|-------------|
| 541–548 | License management | License key validation, trial period, feature-gated tiers (Basic/Pro/Enterprise), license renewal, offline grace period, license transfer, usage analytics, subscription billing |
| 549–556 | Multi-tier plans | Free tier limits, Basic features, Pro features, Enterprise features, plan comparison, upgrade flow, downgrade handling, trial-to-paid conversion |
| 557–567 | White labeling | Custom branding, logo replacement, color theme, app name, splash screen, about screen, receipt branding, installer branding, URL removal, custom help text, custom onboarding |

### G4: Localization (23 features)

| # | Feature | Description |
|---|---------|-------------|
| 568–575 | Indian languages | Hindi, Tamil, Telugu, Kannada, Malayalam, Bengali, Marathi, Gujarati UI translation |
| 576–580 | Regional formats | Indian number format (lakhs/crores), regional date formats, regional calendar, state-wise tax labels, regional receipt templates |
| 581–590 | Translation infrastructure | Resource files (.resx), runtime language switch, RTL support placeholder, translated tooltips, translated validation messages, translated reports, translated receipts, translated email templates, translated help content, language auto-detect |

### G5: Multi-Store (49 features)

| # | Feature | Description |
|---|---------|-------------|
| 591–600 | Store management | Store model, store CRUD, store switch, store-wise data isolation, store config, store-wise users, store address, store GSTIN, store-wise numbering, store hierarchy |
| 601–610 | Inter-store operations | Stock transfer, transfer request/approve, transfer tracking, inter-store pricing, consolidated reports, inter-store returns, shared product catalog, store comparison reports, store-wise dashboard, store-wise P&L |
| 611–620 | Central management | Central admin dashboard, central product master, central price management, central user management, central reports, central settings push, central backup, central alert management, franchise management, royalty tracking |
| 621–639 | Data sync | Cloud sync engine, conflict resolution, offline-first sync, sync queue, sync status, sync retry, selective sync, bandwidth optimization, delta sync, sync log, real-time sync, sync scheduling, sync encryption, multi-device sync, sync health monitor, central database, sync authentication, sync compression, partial sync |

### G6: E-commerce Integration (46 features)

| # | Feature | Description |
|---|---------|-------------|
| 640–652 | Product sync | Push products to Shopify/WooCommerce, pull orders, inventory sync, price sync, image sync, variant sync, category mapping, SKU mapping, bulk sync, sync scheduling, sync error handling, webhook receiver, API rate limiting |
| 653–662 | Order management | Online order list, order → sale conversion, order fulfillment, shipping tracking, order status sync, cancellation handling, return from online, refund processing, multi-channel orders, order priority |
| 663–672 | Marketplace integration | Amazon, Flipkart, Meesho connectors, listing management, order import, inventory push, pricing rules, commission tracking, review management, marketplace analytics |
| 673–685 | Own webstore | Product catalog page, shopping cart, checkout, payment gateway, order notifications, customer accounts, wishlist, search, filters, reviews, SEO basics, mobile responsive, coupon support |

### G7: API & Integration (21 features)

| # | Feature | Description |
|---|---------|-------------|
| 686–692 | REST API | Product API, Sale API, Customer API, Inventory API, API authentication (JWT), API rate limiting, API documentation (Swagger) |
| 693–699 | Accounting integration | Tally export, QuickBooks sync, Zoho Books sync, journal entry export, ledger mapping, auto-posting, reconciliation |
| 700–706 | Communication | SMS API (OTP, alerts), Email API (invoices, reports), WhatsApp Business API, push notifications, webhook support, Slack/Teams alerts, notification templates |

### G8: Mobile Companion (11 features)

| # | Feature | Description |
|---|---------|-------------|
| 707–717 | Mobile app | Dashboard view, stock check, sales summary, barcode scan (camera), price lookup, low stock alerts, daily report, customer lookup, quick sale, offline mode, push notifications |

### G9: Multi-Country (9 features)

| # | Feature | Description |
|---|---------|-------------|
| 718–726 | Country support | Multi-currency, country-specific tax (VAT/GST/Sales Tax), address format, phone format, date/time format, tax registration number format, invoice compliance per country, currency exchange rates, country-specific reports |

### G10: Niche Vertical Extensions (61 features)

| # | Feature | Description |
|---|---------|-------------|
| 727–738 | Tailoring / Alteration | Measurement capture, alteration orders, alteration tracking, delivery date, fitting notes, alteration pricing, alteration receipt, tailor assignment, alteration status, alteration history, alteration charges on bill, alteration reports |
| 739–748 | Rental / Lease | Rental product flag, rental period, rental pricing, return tracking, late fee calc, rental agreement print, deposit management, rental calendar, availability check, rental reports |
| 749–758 | Wholesale / B2B | Wholesale pricing tiers, bulk discounts, credit terms, purchase orders from customers, delivery challan, B2B invoice format, GST B2B compliance, trade discounts, volume pricing, B2B customer portal |
| 759–768 | Consignment | Consignment stock tracking, consignor management, commission calculation, consignment settlement, unsold return, consignment reports, consignment agreement, stock aging per consignor, consignment invoice, consignment reconciliation |
| 769–778 | Fashion seasonal | Season model, season-wise collections, markdown calendar, end-of-season sale, season comparison reports, seasonal inventory planning, lookbook management, trend tracking, season archive, season P&L |
| 779–787 | Gift & loyalty | Gift card issuance, gift card redemption, gift card balance check, gift card expiry, loyalty program rules, points earning rules, points redemption, loyalty tier benefits, loyalty reports |

### G11: Touch & Kiosk (11 features)

| # | Feature | Description |
|---|---------|-------------|
| 788–798 | Touch optimization | Large touch targets, on-screen numpad, swipe gestures, pinch-to-zoom on reports, touch-friendly DataGrid, full-screen kiosk mode, auto-hide taskbar, touch keyboard integration, gesture shortcuts, touch-optimized billing, customer self-checkout kiosk |

### G12: CRM (25 features)

| # | Feature | Description |
|---|---------|-------------|
| 799–808 | Customer engagement | SMS campaigns, email campaigns, birthday greetings auto-send, anniversary offers, abandoned cart reminders, feedback collection, NPS surveys, customer segmentation, targeted promotions, campaign analytics |
| 809–818 | Customer service | Complaint tracking, complaint resolution, service request, follow-up reminders, customer satisfaction score, escalation rules, SLA tracking, service history, warranty tracking, after-sales support |
| 819–823 | Communication templates | SMS templates, email templates, WhatsApp templates, template personalization, template scheduling |

### G13: HR / Staff (8 features)

| # | Feature | Description |
|---|---------|-------------|
| 824–831 | Staff management | Staff attendance, shift scheduling, salary/commission tracking, leave management, performance metrics (sales/hour), staff targets, overtime tracking, payroll export |

### G14: Compliance (17 features)

| # | Feature | Description |
|---|---------|-------------|
| 832–840 | GST compliance | GSTR-1 auto-fill, GSTR-3B summary, GSTR-2A reconciliation, e-Way bill generation, e-Invoice generation (IRN), QR code on invoice (mandate), HSN summary report, tax payment tracker, GST annual return data |
| 841–848 | Legal compliance | Digital signature on invoices, data retention policy, GDPR-like data deletion, consumer protection disclaimers, return policy display, price display regulations, weight/measure compliance, anti-profiteering compliance |

### G15: UI Polish (18 features)

| # | Feature | Description |
|---|---------|-------------|
| 849–858 | Animations & transitions | Page transition animations, form reveal animations, success/error toast notifications, loading skeletons, progress indicators, smooth scrolling, card hover effects, DataGrid row animations, dialog open/close animations, splash screen animation |
| 859–866 | Visual refinements | Icon library (Fluent/Segoe), consistent spacing audit, responsive layout (window resize), high-DPI support, print-specific styles, empty state illustrations, color-coded status badges, data visualization charts |

### G16: DB Administration (14 features)

| # | Feature | Description |
|---|---------|-------------|
| 867–880 | Database tools | DB size monitor, DB vacuum/optimize, migration manager, seed data tool, data purge (old records), archive old sales, index rebuild, query performance log, DB export (JSON), DB import (JSON), schema version display, connection pool monitor, deadlock detection, automatic maintenance |

### G17: Document Management (9 features)

| # | Feature | Description |
|---|---------|-------------|
| 881–889 | Documents | PDF invoice generation, PDF receipt generation, PDF report generation, document storage, document search, document email, document print queue, document template editor, batch document generation |

### G18: Workflow Automation (8 features)

| # | Feature | Description |
|---|---------|-------------|
| 890–897 | Workflows | Low stock → auto PO creation, Day end → auto backup, Sale complete → auto receipt print, Customer credit limit → auto alert, Inventory → auto reorder, Expiry → auto markdown, Milestone → auto loyalty upgrade, Schedule → auto report email |

### G19: User Preferences (19 features)

| # | Feature | Description |
|---|---------|-------------|
| 898–906 | Personal preferences | Default landing page, default view (list/grid), items per page preference, default printer, default payment method, sidebar collapsed/expanded, column visibility per DataGrid, sort preference persistence, filter preference persistence |
| 907–916 | Notification preferences | Email notification toggle, SMS notification toggle, in-app notification toggle, alert sound preference, low stock alert threshold, daily report auto-email, weekly summary preference, notification quiet hours, notification priority filter, notification history |

### G20: Payment Processing (17 features)

| # | Feature | Description |
|---|---------|-------------|
| 917–924 | Payment gateway | Razorpay integration, PayTM integration, PhonePe integration, Google Pay integration, UPI deep link, payment status polling, refund via gateway, payment reconciliation |
| 925–933 | Payment features | EMI support, partial payment tracking, payment plan, advance payment / deposit, payment reminder SMS, payment receipt, payment history per customer, credit/debit note adjustment, foreign currency acceptance |

### G21: Budgeting & Planning (6 features)

| # | Feature | Description |
|---|---------|-------------|
| 934–939 | Financial planning | Monthly sales budget, expense budget, budget vs actual report, variance analysis, forecast based on trends, financial goal setting |

### G22: Advanced Reporting (9 features)

| # | Feature | Description |
|---|---------|-------------|
| 940–948 | Advanced reports | Custom report builder, scheduled report emails, report bookmarks/favorites, comparative reports (period vs period), drill-down reports, report sharing, KPI dashboard, real-time analytics, report access control (role-based) |

---

## Progress Tracking

| Status | Count |
|--------|-------|
| ✅ Built | 11 |
| ⏭ Skipped | 8 |
| 🔨 Remaining | 929 |
| **Total** | **948** |
