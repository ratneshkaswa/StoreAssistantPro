# StoreAssistantPro — All 948 Features (Detailed)

> Every feature numbered individually with description.
> **Status**: ✅ Built | ⏭ Skipped | 🔨 Pending

---

## PHASE 1: PRODUCT FOUNDATION (Sprint 1–5)

### Sprint 1: Product Model Enrichment — ProductsView (27 features)

| # | Feature | P | Status | Description |
|---|---------|---|--------|-------------|
| 1 | Product Name | P0 | ✅ | Required text field (max 200 chars). The primary identifier shown in billing, search, and reports. |
| 2 | Sale Price | P0 | ✅ | Decimal price charged to customer. Supports Indian currency format (₹). Used in cart total calculation. |
| 3 | Quantity (Stock) | P0 | ✅ | Integer stock count per product. Decremented on sale, incremented on return/purchase receive. |
| 4 | Tax Profile FK | P0 | ✅ | Foreign key linking product to a GST tax profile (5%, 12%, 18%, 28%). Drives tax calculation on billing. |
| 5 | HSN Code | P0 | ✅ | 4-8 digit Harmonized System Nomenclature code. Required on GST invoices for goods classification. |
| 6 | Tax Inclusive flag | P0 | ✅ | When true, sale price includes tax (back-calculated). When false, tax is added on top of base price. |
| 7 | RowVersion (concurrency) | P0 | ✅ | EF Core `[Timestamp]` byte array for optimistic concurrency. Prevents overwrite when two users edit simultaneously. |
| 8 | Cost Price | P0 | ✅ | Purchase/landing cost per unit. Used for margin calculation (Profit = SalePrice − CostPrice). |
| 9 | MRP (Max Retail Price) | P0 | ⏭ | Legal maximum retail price printed on product (Indian consumer law). Sale price must not exceed MRP. |
| 10 | SKU / Item Code | P0 | ⏭ | Unique store-assigned alphanumeric code (e.g., "SH-BLK-42"). Used for internal tracking separate from barcode. |
| 11 | Barcode | P0 | ✅ | EAN-13/Code128 barcode string with unique index. Used for scanner-based billing and product lookup. |
| 12 | Unit of Measurement | P1 | ✅ | Editable ComboBox: pcs, meters, sets, pairs, dozen. Determines how quantity is counted and displayed. |
| 13 | Minimum Stock Level | P1 | ✅ | Reorder point integer. When Quantity ≤ MinStockLevel, product appears in low-stock alerts. |
| 14 | Active/Inactive toggle | P1 | ✅ | Boolean flag. Inactive products are hidden from billing search and stock operations but retained in DB. |
| 15 | Description / Notes | P2 | ⏭ | Free-text field (max 500 chars) for internal notes like material, wash instructions, or supplier notes. |
| 16 | Product Image path | P2 | ⏭ | File path to product photo. Displayed as thumbnail in product list and detail views. |
| 17 | Created / Modified date | P2 | ⏭ | Auto-set DateTime fields using IST (IRegionalSettingsService.Now). Never user-editable. |
| 18 | Duplicate / Clone product | P1 | ✅ | Prefills Add form with all fields from selected product. Clears barcode (unique), sets Qty=0, appends "(Copy)". |
| 19 | Search by Barcode | P0 | ✅ | Extends product search to match against Barcode field in addition to Name. Enables scanner-based lookup. |
| 20 | Search by SKU | P0 | ⏭ | Extends product search to match against SKU field. Skipped because SKU field (#10) was skipped. |
| 21 | Filter by Category | P1 | ⏭ | Dropdown in toolbar filters products by category. Deferred until Category model (#28-30) is built. |
| 22 | Filter by Brand | P1 | ⏭ | Dropdown in toolbar filters products by brand. Deferred until Brand model (#32-34) is built. |
| 23 | Filter by Stock Status | P1 | ✅ | Toolbar ComboBox: All/InStock/LowStock/OutOfStock. Server-side filter comparing Qty vs MinStockLevel. |
| 24 | Filter by Active/Inactive | P1 | ✅ | Toolbar ComboBox: All/ActiveOnly/InactiveOnly. Server-side filter on IsActive boolean. |
| 25 | Sale Price ≤ MRP enforcement | P1 | ⏭ | Validation rule preventing sale price from exceeding MRP. Skipped because MRP field (#9) was skipped. |
| 26 | Bulk Import (CSV) | P1 | ✅ | Parse CSV file with headers (Name,SalePrice,CostPrice,Quantity,HSNCode,Barcode,UOM,MinStockLevel). Bulk insert. |
| 27 | Bulk Export (CSV) | P1 | ✅ | Export all products to CSV via SaveFileDialog. Includes all fields with proper CSV escaping. |

### Sprint 2: Categories & Brands — CategoriesView + BrandsView (20 features)

| # | Feature | P | Status | Description |
|---|---------|---|--------|-------------|
| 28 | Category model | P0 | ✅ | Entity: Id, Name (max 100), Description (max 500), IsActive, ParentCategoryId (nullable self-ref). |
| 29 | Category CRUD | P0 | ✅ | Full add/edit/delete with inline forms. Name required, unique within same parent. |
| 30 | Product → Category FK | P0 | ✅ | Add CategoryId (nullable FK) to Product. Products belong to exactly one category or "Uncategorized". |
| 31 | Category hierarchy | P2 | 🔨 | Self-referencing ParentCategoryId enables tree: Clothing → Men → Shirts. TreeView UI. |
| 32 | Brand model | P1 | ✅ | Entity: Id, Name (max 100), LogoPath (nullable), IsActive. |
| 33 | Brand CRUD | P1 | ✅ | Full add/edit/delete with inline forms. Name required, unique. |
| 34 | Product → Brand FK | P1 | ✅ | Add BrandId (nullable FK) to Product. Products optionally belong to one brand. |
| 35 | Category search | P1 | ✅ | Text search by category name in the category list view. |
| 36 | Category paging | P1 | 🔨 | Server-side paging using PagedQuery/PagedResult pattern. |
| 37 | Brand search | P1 | ✅ | Text search by brand name in the brand list view. |
| 38 | Brand paging | P1 | 🔨 | Server-side paging using PagedQuery/PagedResult pattern. |
| 39 | Default category | P2 | 🔨 | Auto-assign new products to "General" or "Uncategorized" category if none selected. |
| 40 | Category product count | P2 | ✅ | Display count of products per category in the category list (e.g., "Shirts (42)"). |
| 41 | Brand product count | P2 | ✅ | Display count of products per brand in the brand list. |
| 42 | Bulk assign category | P2 | 🔨 | Multi-select products in DataGrid → assign to chosen category in one operation. |
| 43 | Bulk assign brand | P2 | 🔨 | Multi-select products in DataGrid → assign to chosen brand in one operation. |
| 44 | Category import (CSV) | P2 | 🔨 | Bulk import categories from CSV file. |
| 45 | Category export (CSV) | P2 | 🔨 | Export all categories to CSV. |
| 46 | Brand import (CSV) | P2 | 🔨 | Bulk import brands from CSV file. |
| 47 | Brand export (CSV) | P2 | 🔨 | Export all brands to CSV. |

### Sprint 3: Product Variants & Sizes — ProductsView enrichment (18 features)

| # | Feature | P | Status | Description |
|---|---------|---|--------|-------------|
| 48 | Size model | P0 | ✅ | Entity: Id, Name (S/M/L/XL/XXL/Free Size), SortOrder. Master table for clothing sizes. |
| 49 | Color model | P0 | ✅ | Entity: Id, Name (Black/White/Red…), HexCode (#000000). Master table for colors. |
| 50 | Product Variant model | P0 | ✅ | Entity: Id, ProductId (FK), SizeId (FK), ColorId (FK), Barcode (unique), Quantity, AdditionalPrice. |
| 51 | Variant CRUD | P0 | ✅ | Add/edit/delete variants per product. Each variant is a unique size+color combination. |
| 52 | Variant stock tracking | P0 | ✅ | Each variant maintains its own quantity independent of parent product. |
| 53 | Variant barcode (unique) | P0 | ✅ | Every variant gets a unique barcode. Scanning adds that specific variant to cart. |
| 54 | Variant price adjustment | P1 | ✅ | ± price offset from base product price. E.g., XXL costs ₹50 extra: AdditionalPrice = 50. |
| 55 | Size master CRUD | P1 | ✅ | Manage the master list of available sizes. Add custom sizes (28, 30, 32 for trousers). |
| 56 | Color master CRUD | P1 | ✅ | Manage the master list of available colors with hex codes for UI display. |
| 57 | Size group templates | P2 | 🔨 | Predefined groups: "Shirt sizes" (S-XXL), "Trouser sizes" (28-40), "Free size". Quick-fill variants. |
| 58 | Variant grid view | P1 | 🔨 | Matrix view: rows=sizes, columns=colors, cells=quantity. Visual stock overview per product. |
| 59 | Variant search in billing | P0 | ✅ | Scan variant barcode → auto-add to cart with correct size/color/price. |
| 60 | Variant low stock alert | P1 | 🔨 | Per-variant minimum stock check. Alert when any variant falls below threshold. |
| 61 | Bulk variant creation | P1 | ✅ | Select multiple sizes + multiple colors → auto-create all combinations (e.g., 5 sizes × 4 colors = 20 variants). |
| 62 | Variant import (CSV) | P2 | 🔨 | Bulk import variants with columns: ProductName, Size, Color, Barcode, Qty, PriceOffset. |
| 63 | Variant export (CSV) | P2 | 🔨 | Export all variants to CSV for backup/analysis. |
| 64 | Variant image per color | P2 | 🔨 | Different product photo per color variant for visual identification. |
| 65 | Variant inactive toggle | P2 | ✅ | Deactivate specific size/color combos without deleting (e.g., discontinued size). |

### Sprint 4: Inventory Basics — InventoryView (15 features)

| # | Feature | P | Status | Description |
|---|---------|---|--------|-------------|
| 66 | Stock adjustment (manual +/-) | P0 | ✅ | Increase or decrease product quantity with a reason. Positive = stock in, negative = stock out. |
| 67 | Adjustment reason codes | P0 | ✅ | Predefined reasons: Damage, Theft, Correction, Return, Transfer, Opening Stock, Other. |
| 68 | Stock adjustment log | P0 | ✅ | Immutable audit trail: ProductId, OldQty, NewQty, Reason, UserId, Timestamp. |
| 69 | Stock take / physical count | P1 | 🔨 | Enter physical count per product. System auto-calculates difference and creates adjustment. |
| 70 | Low stock alert dashboard | P0 | ✅ | Dedicated view listing all products where Quantity ≤ MinStockLevel. Sorted by urgency. |
| 71 | Out of stock alert | P0 | ✅ | Separate section/badge for products where Quantity = 0. Highlighted in red. |
| 72 | Stock value report | P1 | ✅ | Total inventory value calculated as Σ(Quantity × CostPrice) across all products. |
| 73 | Stock movement history | P1 | 🔨 | Timeline per product showing all quantity changes: sales, returns, adjustments, purchases. |
| 74 | Opening stock entry | P1 | 🔨 | Bulk set initial stock quantities when first onboarding the system. |
| 75 | Stock freeze (period lock) | P2 | 🔨 | Lock all stock edits during physical audit period. Only admin can freeze/unfreeze. |
| 76 | Batch stock adjustment | P1 | 🔨 | Adjust multiple products in one screen. DataGrid with Product/CurrentQty/NewQty/Reason. |
| 77 | Stock adjustment approval | P2 | 🔨 | Adjustments above configurable threshold require manager PIN approval. |
| 78 | Inventory export (CSV) | P1 | 🔨 | Export current stock snapshot: Product, Qty, CostPrice, Value, MinStock, Status. |
| 79 | Inventory import (CSV) | P1 | 🔨 | Bulk update stock quantities from CSV file. Creates adjustment log entries. |
| 80 | Dead stock report | P2 | 🔨 | Products with zero sales in configurable N days. Helps identify unsold inventory. |

### Sprint 5: Supplier / Vendor Management — SuppliersView (15 features)

| # | Feature | P | Status | Description |
|---|---------|---|--------|-------------|
| 81 | Supplier model | P0 | ✅ | Entity: Id, Name, ContactPerson, Phone, Email, GSTIN (15 chars), Address, IsActive, Notes. |
| 82 | Supplier CRUD | P0 | ✅ | Full add/edit/delete with inline forms, paging, search. |
| 83 | Product → Supplier FK | P1 | ✅ | Add SupplierId (nullable FK) to Product. Links product to primary supplier. |
| 84 | Supplier active/inactive | P1 | ✅ | Deactivate suppliers without deleting. Inactive suppliers hidden from dropdowns. |
| 85 | Supplier search | P1 | ✅ | Search by name, phone number, or GSTIN. |
| 86 | Supplier GSTIN validation | P1 | 🔨 | Validate 15-character Indian GSTIN format: 2-digit state + 10-char PAN + 1Z + checksum. |
| 87 | Supplier ledger | P2 | 🔨 | Running balance per supplier: total purchases − total payments = outstanding. |
| 88 | Supplier import (CSV) | P2 | 🔨 | Bulk import suppliers from CSV. |
| 89 | Supplier export (CSV) | P2 | 🔨 | Export all suppliers to CSV. |
| 90 | Supplier payment tracking | P2 | 🔨 | Record payments made to supplier with date, amount, payment method, reference. |
| 91 | Supplier due alerts | P2 | 🔨 | Dashboard alert when supplier payment is overdue beyond agreed credit period. |
| 92 | Multiple suppliers per product | P2 | 🔨 | Many-to-many: ProductSupplier join table. Multiple vendors can supply same product. |
| 93 | Supplier price list | P2 | 🔨 | Cost price varies per supplier per product. Pick best price when creating PO. |
| 94 | Supplier notes | P2 | ✅ | Free-text notes field per supplier for special terms, delivery schedules, etc. |
| 95 | Supplier product count | P2 | 🔨 | Display number of products linked to each supplier in the supplier list. |

---

## PHASE 2: BILLING & SALES (Sprint 6–10)

### Sprint 6: Billing Core — SalesView enrichment (20 features)

| # | Feature | P | Status | Description |
|---|---------|---|--------|-------------|
| 96 | Add product by name search | P0 | 🔨 | Type-ahead search box in billing. Type product name → select from dropdown → add to cart. |
| 97 | Add product by barcode scan | P0 | 🔨 | Barcode scanner input detected → product looked up → instantly added to cart with qty=1. |
| 98 | Cart line: quantity edit | P0 | 🔨 | Click qty field in cart → type new quantity. Auto-recalculates line total and bill total. |
| 99 | Cart line: remove item | P0 | 🔨 | Delete button per line item. Removes product from cart entirely. |
| 100 | Cart line: price override | P1 | 🔨 | Change sale price for this transaction only. Requires manager PIN if discount > threshold. |
| 101 | Cart subtotal display | P0 | 🔨 | Live sum of all line totals (before tax and discount). Updates on every cart change. |
| 102 | Cart tax display | P0 | 🔨 | Total tax amount. Shows CGST + SGST (or IGST) calculated per line based on tax profile. |
| 103 | Cart grand total | P0 | 🔨 | Final amount: Subtotal + Tax − Discount. Large, prominent display. |
| 104 | Bill-level discount (₹) | P0 | 🔨 | Flat rupee discount applied to entire bill. Distributed proportionally across line items for GST. |
| 105 | Bill-level discount (%) | P0 | 🔨 | Percentage discount on bill total. Converted to ₹ amount then distributed. |
| 106 | Line-level discount (₹) | P1 | 🔨 | Flat rupee discount on a specific line item. Reduces that item's taxable value. |
| 107 | Line-level discount (%) | P1 | 🔨 | Percentage discount on a specific line item. |
| 108 | Payment: Cash | P0 | 🔨 | Select Cash payment method. Enter amount tendered. |
| 109 | Cash change calculation | P0 | 🔨 | If cash tendered > grand total, display change due = tendered − total. |
| 110 | Complete sale → deduct stock | P0 | 🔨 | Atomic transaction: create Sale record + SaleItems + decrement product quantities. |
| 111 | Sale number auto-generation | P0 | 🔨 | Sequential invoice number with configurable prefix (e.g., INV-2025-00001). |
| 112 | Sale timestamp (IST) | P0 | 🔨 | Record sale date/time using IRegionalSettingsService.Now (Indian Standard Time). |
| 113 | Sale → User tracking | P0 | 🔨 | Record which user (cashier) completed the sale. FK to UserCredential. |
| 114 | Cart clear / cancel bill | P0 | 🔨 | Discard entire cart. Confirmation dialog required. No stock changes on cancel. |
| 115 | Billing keyboard shortcuts | P0 | 🔨 | F2=New bill, F5=Pay, F8=Hold, Esc=Cancel, +/- for qty, Tab through fields. |

### Sprint 7: Payment Methods & Receipts — SalesView + ReceiptView (20 features)

| # | Feature | P | Status | Description |
|---|---------|---|--------|-------------|
| 116 | Payment: UPI | P0 | 🔨 | Record UPI transaction reference number. No gateway integration (manual entry). |
| 117 | Payment: Card | P0 | 🔨 | Record card type (credit/debit) and last 4 digits. Manual entry, no POS terminal integration. |
| 118 | Payment: Split payment | P1 | 🔨 | Part cash + part UPI/card. Multiple payment entries summing to grand total. |
| 119 | Payment: Credit (on account) | P1 | 🔨 | Sale on customer credit. Creates receivable balance against customer. |
| 120 | Receipt: thermal 80mm | P0 | 🔨 | Format receipt for standard 80mm thermal printer. Fixed-width text layout. |
| 121 | Receipt: firm details | P0 | 🔨 | Print store name, address, phone on receipt header. |
| 122 | Receipt: GSTIN | P0 | 🔨 | Print store's GST identification number on receipt. |
| 123 | Receipt: line items | P0 | 🔨 | Print each item: name, qty, rate, discount, tax, line total. |
| 124 | Receipt: totals summary | P0 | 🔨 | Print subtotal, discount, tax (CGST/SGST), grand total. |
| 125 | Receipt: payment method | P0 | 🔨 | Print how customer paid: Cash/UPI/Card with reference. |
| 126 | Receipt: thank you footer | P1 | 🔨 | Configurable footer text: "Thank you! Visit again!" |
| 127 | Receipt preview | P1 | 🔨 | On-screen preview before sending to printer. |
| 128 | Receipt reprint | P1 | 🔨 | Reprint receipt for any past sale from sale history. |
| 129 | Receipt: QR code | P2 | 🔨 | QR code containing sale ID or UPI payment link. |
| 130 | Receipt: invoice barcode | P2 | 🔨 | Barcode of invoice number for easy scanning/lookup. |
| 131 | A4 tax invoice format | P1 | 🔨 | Full-page GST tax invoice for B2B customers with all required fields. |
| 132 | Invoice: HSN-wise tax summary | P0 | 🔨 | GST-compliant table grouping tax by HSN code (mandatory for invoices > ₹50K). |
| 133 | Invoice: CGST/SGST/IGST split | P0 | 🔨 | Show tax components separately: CGST=9%, SGST=9% (intra-state) or IGST=18% (inter-state). |
| 134 | Invoice: customer details | P1 | 🔨 | Print buyer name, address, GSTIN, state code on B2B invoice. |
| 135 | Email invoice (PDF) | P2 | 🔨 | Generate PDF invoice and send as email attachment to customer. |

### Sprint 8: Sale History & Returns — SalesView + ReturnsView (18 features)

| # | Feature | P | Status | Description |
|---|---------|---|--------|-------------|
| 136 | Sale history list | P0 | 🔨 | Paged DataGrid of all past sales: Invoice#, Date, Customer, Total, PaymentMethod. |
| 137 | Sale history: date filter | P0 | 🔨 | DatePicker range filter to view sales within specific period. |
| 138 | Sale history: invoice search | P0 | 🔨 | Search by invoice number for quick lookup. |
| 139 | Sale detail view | P0 | 🔨 | Click sale → expand/dialog showing all line items, taxes, payment details. |
| 140 | Sale history export (CSV) | P1 | 🔨 | Export filtered sales data to CSV file. |
| 141 | Sale return (full) | P0 | 🔨 | Return entire sale: reverse all line items, restore stock, calculate refund. |
| 142 | Sale return (partial) | P0 | 🔨 | Select specific items/quantities to return from a sale. |
| 143 | Return reason code | P1 | 🔨 | Mandatory reason: Defective, Wrong Size, Customer Changed Mind, Duplicate, Other. |
| 144 | Return → stock restoration | P0 | 🔨 | Automatically add returned quantities back to product stock. |
| 145 | Return → refund calculation | P0 | 🔨 | Calculate refund amount based on original prices of returned items including tax. |
| 146 | Credit note generation | P1 | 🔨 | Issue credit note document for returns. Links back to original invoice. |
| 147 | Exchange (return + new sale) | P1 | 🔨 | Combined flow: return items from old sale + bill new items. Net amount = new − returned. |
| 148 | Return approval (Manager PIN) | P1 | 🔨 | Returns require manager authorization via PIN entry. |
| 149 | Return history list | P1 | 🔨 | Paged list of all processed returns with details. |
| 150 | Return receipt print | P1 | 🔨 | Print return/credit note receipt for customer. |
| 151 | Daily return summary | P2 | 🔨 | Aggregate return count and value per day. |
| 152 | Return against original invoice | P0 | 🔨 | Every return must reference original sale ID. Prevents duplicate returns. |
| 153 | No-bill return (manager only) | P2 | 🔨 | Accept return without original invoice. Admin-only, logged in audit trail. |

### Sprint 9: Customer Management — CustomersView (20 features)

| # | Feature | P | Status | Description |
|---|---------|---|--------|-------------|
| 154 | Customer model | P0 | 🔨 | Entity: Id, Name, Phone, Email, Address, GSTIN, CreatedDate, IsActive, Notes. |
| 155 | Customer CRUD | P0 | 🔨 | Full add/edit/delete with inline forms, search, paging. |
| 156 | Customer search by phone | P0 | 🔨 | Quick lookup by 10-digit mobile number. Most common customer identifier in India. |
| 157 | Customer → Sale linking | P0 | 🔨 | Attach customer to a sale at billing time. Enables purchase history and credit tracking. |
| 158 | Walk-in customer (default) | P0 | 🔨 | Sales without a named customer use implicit "Walk-in" customer. No data collected. |
| 159 | Customer purchase history | P1 | 🔨 | View all past purchases by a customer with dates and amounts. |
| 160 | Customer outstanding balance | P1 | 🔨 | Total unpaid credit: Σ(credit sales) − Σ(payments received). |
| 161 | Customer payment collection | P1 | 🔨 | Record payment received against outstanding balance. Date, amount, method, reference. |
| 162 | Customer loyalty points | P2 | 🔨 | Earn points per ₹ spent (configurable ratio). Accumulate and redeem on future purchases. |
| 163 | Customer tier | P2 | 🔨 | Auto-tier based on total spend: Regular → Silver (₹10K+) → Gold (₹50K+). Tier-specific discounts. |
| 164 | Customer birthday | P2 | 🔨 | Date field. Enables birthday discount campaigns and SMS/WhatsApp greetings. |
| 165 | Customer anniversary | P2 | 🔨 | Date field. Enables anniversary promotions. |
| 166 | Customer notes | P2 | 🔨 | Free-text notes: preferences, special instructions, relationship notes. |
| 167 | Customer import (CSV) | P2 | 🔨 | Bulk import customer list from CSV file. |
| 168 | Customer export (CSV) | P2 | 🔨 | Export all customers to CSV. |
| 169 | Customer duplicate detection | P1 | 🔨 | Warn when adding customer with phone number that already exists. |
| 170 | Customer active/inactive | P1 | 🔨 | Deactivate customers without deleting. Preserves history. |
| 171 | Customer GSTIN validation | P1 | 🔨 | Validate 15-char GSTIN format for B2B customers. |
| 172 | Customer group/tag | P2 | 🔨 | Tag customers: "Wholesale", "Regular", "VIP". Filter and target by group. |
| 173 | Customer credit limit | P2 | 🔨 | Maximum outstanding balance allowed. Block credit sales beyond limit. |

### Sprint 10: Discount & Promotion Engine — DiscountsView (17 features)

| # | Feature | P | Status | Description |
|---|---------|---|--------|-------------|
| 174 | Flat discount on bill | P0 | 🔨 | Enter ₹ amount off total bill. Distributed across items for GST calculation. |
| 175 | Percentage discount on bill | P0 | 🔨 | Enter % off total bill. Converted to ₹ then distributed. |
| 176 | Line-item flat discount | P0 | 🔨 | ₹ off a specific line item in the cart. |
| 177 | Line-item % discount | P0 | 🔨 | % off a specific line item. |
| 178 | Discount approval (PIN) | P1 | 🔨 | Manager PIN required for discounts exceeding configurable threshold (e.g., >20%). |
| 179 | Maximum discount limit | P1 | 🔨 | System-wide cap on maximum discount percentage allowed. Prevents excessive discounts. |
| 180 | Buy X Get Y free | P2 | 🔨 | Promotional rule: "Buy 2 shirts, get 1 free". Auto-applies when cart matches criteria. |
| 181 | Combo / bundle pricing | P2 | 🔨 | Special price when specific products bought together: "Shirt + Trouser = ₹1999". |
| 182 | Seasonal sale discount | P2 | 🔨 | Time-bound discount: start date → end date. Auto-applies during active period. |
| 183 | Category-wide discount | P2 | 🔨 | Apply % discount to all products in a category (e.g., "20% off all T-shirts"). |
| 184 | Brand-wide discount | P2 | 🔨 | Apply % discount to all products of a brand. |
| 185 | Customer-specific discount | P2 | 🔨 | Special pricing for specific customers (wholesale rates, loyalty discounts). |
| 186 | Coupon code support | P2 | 🔨 | Enter alphanumeric code at billing → validate → apply associated discount. |
| 187 | Discount history log | P1 | 🔨 | Audit trail of every discount given: who, when, how much, on which sale. |
| 188 | Discount report (daily) | P1 | 🔨 | Total discount value given per day. Helps monitor discount leakage. |
| 189 | Minimum bill for discount | P2 | 🔨 | Discount only applies if bill total exceeds threshold (e.g., "10% off on bills > ₹2000"). |
| 190 | Discount stacking rules | P2 | 🔨 | Control whether multiple discounts can combine (e.g., coupon + seasonal). |

---

## PHASE 3: FINANCIAL & TAX (Sprint 11–15)

### Sprint 11: GST Tax Engine — TaxManagementWindow enrichment (18 features)

| # | Feature | P | Status | Description |
|---|---------|---|--------|-------------|
| 191 | Tax Profile CRUD | P0 | ✅ | Create/edit/delete tax rate profiles. Each profile has a name and rate (e.g., "GST 18%" = 18%). |
| 192 | CGST + SGST split | P0 | ✅ | Intra-state: tax rate divided equally. 18% → CGST 9% + SGST 9%. |
| 193 | IGST for inter-state | P0 | ✅ | Inter-state: full rate as IGST. 18% → IGST 18%. |
| 194 | Tax-inclusive back-calculation | P0 | ✅ | Extract base price from inclusive price: Base = Price / (1 + Rate). |
| 195 | Tax-exclusive add-on | P0 | ✅ | Add tax on top: Total = Base × (1 + Rate). |
| 196 | HSN-wise tax summary | P0 | 🔨 | Group tax amounts by HSN code on invoice. Required for GST compliance. |
| 197 | Cess support | P1 | 🔨 | Additional cess percentage on top of GST for specific goods (luxury items). |
| 198 | Tax exemption flag | P1 | 🔨 | Mark specific products as tax-exempt (0% GST). |
| 199 | Composition scheme | P2 | 🔨 | Flat tax rate for small businesses (< ₹1.5 Cr turnover). Different invoice format. |
| 200 | Reverse charge mechanism | P2 | 🔨 | Buyer pays tax directly to government instead of seller collecting it. |
| 201 | Tax profile active/inactive | P1 | ✅ | Deactivate obsolete tax rates. Inactive profiles hidden from product assignment. |
| 202 | Default tax profile | P1 | ✅ | One profile marked as default. Auto-assigned to new products. |
| 203 | Tax rate change audit | P1 | 🔨 | Log when tax rates are modified: old rate, new rate, who changed, when. |
| 204 | Multi-tax per product | P2 | 🔨 | Different tax rates for different states (inter-state vs intra-state). |
| 205 | Tax report (monthly) | P1 | 🔨 | Total tax collected per month: CGST + SGST + IGST breakdown. |
| 206 | Tax report by HSN | P1 | 🔨 | Tax grouped by HSN code for GST return filing. |
| 207 | GSTR-1 data export | P2 | 🔨 | Export sales data in GSTR-1 format for monthly GST return. |
| 208 | GSTR-3B summary | P2 | 🔨 | Generate summary matching GSTR-3B format for monthly GST payment. |

### Sprint 12: Purchase Orders — PurchaseOrdersView (16 features)

| # | Feature | P | Status | Description |
|---|---------|---|--------|-------------|
| 209 | Purchase Order model | P1 | 🔨 | Entity: Id, PONumber, SupplierId, OrderDate, ExpectedDate, Status, Notes, TotalAmount. |
| 210 | PO creation | P1 | 🔨 | Create PO with supplier selection and line items (product, qty, unit cost). |
| 211 | PO → Supplier link | P1 | 🔨 | Each PO linked to exactly one supplier. |
| 212 | PO line items | P1 | 🔨 | POItem: POId, ProductId, QtyOrdered, QtyReceived, UnitCost, LineTotal. |
| 213 | PO status workflow | P1 | 🔨 | Status transitions: Draft → Sent → Partial Received → Fully Received → Closed/Cancelled. |
| 214 | PO receive goods | P1 | 🔨 | Mark items as received → automatically updates product stock quantities. |
| 215 | PO partial receipt | P1 | 🔨 | Receive some items now, rest later. QtyReceived updated incrementally. |
| 216 | PO cost price update | P1 | 🔨 | Update product cost price from PO unit cost on receipt. |
| 217 | PO search / filter | P1 | 🔨 | Search by PO number, supplier name, date range, status. |
| 218 | PO paging | P1 | 🔨 | Server-side paging for PO list. |
| 219 | PO print | P2 | 🔨 | Print formatted purchase order to send to supplier. |
| 220 | PO history | P1 | 🔨 | View all past purchase orders with status and amounts. |
| 221 | PO duplicate / reorder | P2 | 🔨 | Clone a previous PO to quickly reorder same products from same supplier. |
| 222 | PO auto-generate from low stock | P2 | 🔨 | System suggests PO with products below min stock level, grouped by supplier. |
| 223 | PO import (CSV) | P2 | 🔨 | Create PO from CSV file with product/qty/cost data. |
| 224 | PO export (CSV) | P2 | 🔨 | Export PO details to CSV. |

### Sprint 13: Expense Tracking — ExpensesView (14 features)

| # | Feature | P | Status | Description |
|---|---------|---|--------|-------------|
| 225 | Expense model | P1 | 🔨 | Entity: Id, Date, CategoryId, Amount, PaymentMethod, Description, CreatedBy, ReceiptPath. |
| 226 | Expense CRUD | P1 | 🔨 | Add/edit/delete daily expenses with amount, category, and notes. |
| 227 | Expense categories | P1 | 🔨 | Predefined: Rent, Utilities, Salary, Transport, Packaging, Marketing, Maintenance, Misc. |
| 228 | Expense category CRUD | P1 | 🔨 | Add/edit/delete custom expense categories. |
| 229 | Expense search by date | P1 | 🔨 | Filter expenses by date range. |
| 230 | Expense search by category | P1 | 🔨 | Filter expenses by category dropdown. |
| 231 | Daily expense summary | P1 | 🔨 | Total expenses for today, grouped by category. |
| 232 | Monthly expense report | P1 | 🔨 | Monthly aggregate by category with totals. |
| 233 | Expense receipt photo | P2 | 🔨 | Attach photo/scan of physical receipt to expense record. |
| 234 | Recurring expense | P2 | 🔨 | Auto-create monthly expenses like rent. Template with amount and due date. |
| 235 | Expense approval | P2 | 🔨 | Expenses above threshold require manager approval. |
| 236 | Expense paging | P1 | 🔨 | Server-side paging for expense list. |
| 237 | Expense import (CSV) | P2 | 🔨 | Bulk import historical expenses from CSV. |
| 238 | Expense export (CSV) | P2 | 🔨 | Export expenses to CSV for accounting software import. |

### Sprint 14: Cash Register / Day End — CashRegisterView (16 features)

| # | Feature | P | Status | Description |
|---|---------|---|--------|-------------|
| 239 | Opening cash balance | P0 | 🔨 | Record cash physically present in drawer at start of business day. |
| 240 | Cash in / cash out entries | P0 | 🔨 | Record non-sale cash movements: petty cash out for supplies, cash deposits from owner. |
| 241 | Cash in/out reason | P0 | 🔨 | Mandatory reason for each cash movement: Petty Cash, Bank Deposit, Change Fund, Other. |
| 242 | Expected cash balance | P0 | 🔨 | Calculated: Opening + Cash Sales − Cash Returns − Cash Out + Cash In. |
| 243 | Actual cash count | P0 | 🔨 | User enters physical cash count at end of day. |
| 244 | Cash variance | P0 | 🔨 | Difference: Actual Count − Expected Balance. Positive = excess, negative = shortage. |
| 245 | Day end report | P0 | 🔨 | Summary: total sales, returns, payments by method, expenses, opening/closing cash. |
| 246 | Day close / lockdown | P1 | 🔨 | Close the business day. No more transactions for that date after close. |
| 247 | Day-wise cash history | P1 | 🔨 | View past day summaries: opening, closing, variance per day. |
| 248 | Payment method breakdown | P1 | 🔨 | Day end shows: Cash ₹X, UPI ₹Y, Card ₹Z, Credit ₹W. |
| 249 | Denomination entry | P2 | 🔨 | Count by denomination: ₹2000×__, ₹500×__, ₹200×__, ₹100×__, ₹50×__, coins. Auto-sum. |
| 250 | Shift support | P2 | 🔨 | Multiple shifts per day. Each cashier has own open/close with handover amount. |
| 251 | Cash register export | P2 | 🔨 | Export day summary to CSV/PDF. |
| 252 | Cash variance alert | P1 | 🔨 | Warning notification if variance exceeds configurable threshold (e.g., ₹100). |
| 253 | Cash register approval | P2 | 🔨 | Manager must sign off on day close. Verify variance is acceptable. |
| 254 | Cash register audit log | P1 | 🔨 | Immutable log of every cash movement: type, amount, reason, user, timestamp. |

### Sprint 15: Profit & Loss — ReportsView (16 features)

| # | Feature | P | Status | Description |
|---|---------|---|--------|-------------|
| 255 | Daily sales summary | P0 | 🔨 | Total sales, total returns, net sales, transaction count for today. |
| 256 | Monthly sales summary | P0 | 🔨 | Aggregate sales by month. Revenue, COGS, gross profit. |
| 257 | Product-wise sales report | P0 | 🔨 | Per product: qty sold, revenue, cost, profit, margin %. |
| 258 | Category-wise sales report | P1 | 🔨 | Revenue, qty, profit grouped by product category. |
| 259 | Brand-wise sales report | P1 | 🔨 | Revenue, qty, profit grouped by product brand. |
| 260 | Profit margin per product | P1 | 🔨 | (SalePrice − CostPrice) / SalePrice × 100 per product. |
| 261 | Gross profit report | P1 | 🔨 | Total Revenue − Total COGS for selected period. |
| 262 | Net profit | P1 | 🔨 | Gross Profit − Total Expenses for selected period. |
| 263 | Best selling products | P1 | 🔨 | Top N products by quantity sold or revenue in period. |
| 264 | Slow moving products | P1 | 🔨 | Bottom N products by sales velocity. Helps plan markdowns. |
| 265 | Sales by user report | P1 | 🔨 | Revenue and transaction count per salesperson/cashier. |
| 266 | Sales by payment method | P1 | 🔨 | Breakdown: Cash vs UPI vs Card vs Credit per period. |
| 267 | Sales by hour | P2 | 🔨 | Hourly sales distribution. Identifies peak shopping hours. |
| 268 | Customer-wise sales report | P2 | 🔨 | Revenue per customer, sorted by total spend. |
| 269 | Report date range picker | P0 | 🔨 | DatePicker UI for custom date range on all reports. Presets: Today, Week, Month, Quarter, Year. |
| 270 | Report export (CSV) | P1 | 🔨 | Export any report to CSV file for further analysis. |

---

## PHASE 4: USERS & SECURITY (Sprint 16–19)

### Sprint 16: User Role Enrichment — UserManagementWindow (18 features)

| # | Feature | P | Status | Description |
|---|---------|---|--------|-------------|
| 271 | Admin role | P0 | ✅ | Full access: all settings, user management, destructive operations. |
| 272 | Manager role | P0 | ✅ | All business operations: products, billing, reports. No system settings. |
| 273 | User/Cashier role | P0 | ✅ | Billing only. Cannot add/edit products or view reports. |
| 274 | Role-based menu visibility | P0 | ✅ | Sidebar menu items show/hide based on logged-in user's role. |
| 275 | Role-based button visibility | P0 | ✅ | Action buttons (Delete, Edit) visible only to authorized roles. |
| 276 | PIN change (self) | P0 | ✅ | User can change their own 4-digit PIN. Must enter current PIN first. |
| 277 | PIN reset (admin) | P0 | ✅ | Admin resets another user's PIN without knowing old PIN. |
| 278 | User activity log | P1 | 🔨 | Record login/logout times per user session. |
| 279 | User session tracking | P1 | 🔨 | Show currently logged-in user on status bar. Track session duration. |
| 280 | User sales performance | P1 | 🔨 | Sales totals per user for performance evaluation. |
| 281 | User profile | P2 | 🔨 | Display name, optional photo, contact info. |
| 282 | Multiple users per role | P0 | ✅ | Multiple cashiers, multiple managers supported. |
| 283 | User active/inactive | P1 | ✅ | Deactivate users (prevent login) without deleting records. |
| 284 | User creation | P0 | ✅ | Admin creates new users with name, role, and PIN. |
| 285 | User deletion | P0 | ✅ | Soft-delete or deactivate. Preserve audit history. |
| 286 | User search | P1 | ✅ | Search users by name in user management. |
| 287 | User import (CSV) | P2 | 🔨 | Bulk create users from CSV. |
| 288 | User export (CSV) | P2 | 🔨 | Export user list to CSV. |

### Sprint 17: Permissions & Audit — SecuritySettingsView (16 features)

| # | Feature | P | Status | Description |
|---|---------|---|--------|-------------|
| 289 | Permission matrix | P1 | 🔨 | Configurable grid: Role × Feature = Allow/Deny. Fine-grained access control. |
| 290 | Master PIN for destructive ops | P0 | ✅ | 6-digit master PIN required for: delete product, delete user, factory reset. |
| 291 | Audit log model | P1 | 🔨 | Entity: Id, Action, EntityType, EntityId, OldValue, NewValue, UserId, Timestamp. |
| 292 | Audit: product changes | P1 | 🔨 | Log every product create/update/delete with before/after values. |
| 293 | Audit: sale completion | P0 | 🔨 | Log every completed sale: invoice#, total, user, timestamp. |
| 294 | Audit: price override | P0 | 🔨 | Log when cashier changes price during billing: product, old price, new price, reason. |
| 295 | Audit: discount given | P0 | 🔨 | Log every discount: type, amount, percentage, approved by. |
| 296 | Audit: return processed | P1 | 🔨 | Log returns: original invoice, returned items, refund amount, reason. |
| 297 | Audit: login/logout | P1 | 🔨 | Track user sessions: login time, logout time, duration. |
| 298 | Audit: settings changed | P1 | 🔨 | Log when firm details, tax rates, or system settings are modified. |
| 299 | Audit log viewer | P1 | 🔨 | Searchable, paged DataGrid with filters by action type, user, date range. |
| 300 | Audit log export | P2 | 🔨 | Export filtered audit log to CSV. |
| 301 | Failed login tracking | P1 | 🔨 | Count consecutive failed PIN attempts per user. |
| 302 | Account lockout | P2 | 🔨 | Lock user after N consecutive failed attempts. Require admin unlock. |
| 303 | Session timeout | P2 | 🔨 | Auto-logout after configurable inactivity period (e.g., 15 minutes). |
| 304 | PIN complexity rules | P2 | 🔨 | Reject weak PINs: 1234, 0000, 1111, same digit repeated. Already partially built. |

### Sprint 18: Firm / Store Settings — FirmManagementWindow (16 features)

| # | Feature | P | Status | Description |
|---|---------|---|--------|-------------|
| 305 | Firm name | P0 | ✅ | Store name displayed on receipts, invoices, and title bar. |
| 306 | Firm address | P0 | ✅ | Full postal address for invoices and receipts. |
| 307 | Firm phone | P0 | ✅ | Contact phone number on receipts. |
| 308 | Firm GSTIN | P0 | ✅ | GST Identification Number. Printed on all tax invoices. |
| 309 | Firm PAN | P1 | 🔨 | Permanent Account Number for income tax compliance. |
| 310 | Firm email | P1 | ✅ | Contact email for customer communication. |
| 311 | Firm logo | P1 | 🔨 | Image file used on receipts, invoices, and A4 letterhead. |
| 312 | Firm bank details | P2 | 🔨 | Bank name, account number, IFSC code for payment receipts. |
| 313 | Invoice prefix config | P1 | 🔨 | Custom prefix for invoice numbers (e.g., "SA-", "INV-2025-"). |
| 314 | Invoice numbering reset | P2 | 🔨 | Reset sequence counter annually (April 1) or monthly. |
| 315 | Receipt footer text | P1 | 🔨 | Configurable text printed at bottom of every receipt. |
| 316 | Receipt header text | P2 | 🔨 | Additional text above store name on receipt. |
| 317 | Currency symbol | P1 | 🔨 | Default ₹ (Indian Rupee). Configurable for other currencies. |
| 318 | Financial year start | P1 | 🔨 | April 1 default (Indian FY). Affects report period boundaries. |
| 319 | Decimal places | P2 | 🔨 | Round to 0 or 2 decimal places in billing/display. |
| 320 | Store hours | P2 | 🔨 | Opening/closing time. Display on receipts, enable time-based features. |

### Sprint 19: Backup & Data Safety — BackupSettingsView (15 features)

| # | Feature | P | Status | Description |
|---|---------|---|--------|-------------|
| 321 | Manual database backup | P0 | 🔨 | One-click button copies SQLite DB file to chosen location with timestamp in filename. |
| 322 | Auto backup on day close | P0 | 🔨 | Automatically create backup when day-end process completes. |
| 323 | Backup location config | P0 | 🔨 | FolderBrowserDialog to choose backup destination folder. |
| 324 | Backup history list | P1 | 🔨 | DataGrid showing past backups: date, size, location, status. |
| 325 | Restore from backup | P0 | 🔨 | Select backup file → confirm → replace current DB. Requires master PIN. |
| 326 | Backup to USB | P1 | 🔨 | Detect USB drives, one-click backup to removable media. |
| 327 | Scheduled backup | P2 | 🔨 | Background timer creates backup every N hours automatically. |
| 328 | Backup encryption | P2 | 🔨 | Password-protect backup files using AES encryption. |
| 329 | Cloud backup | P2 | 🔨 | Upload backup to OneDrive/Google Drive via API. |
| 330 | Backup size tracking | P2 | 🔨 | Show file sizes. Warn if backup folder exceeds threshold. |
| 331 | Old backup cleanup | P2 | 🔨 | Auto-delete backups older than configurable N days. |
| 332 | Backup reminder | P1 | 🔨 | Dashboard notification if no backup exists in last 24 hours. |
| 333 | Full data export (JSON/CSV) | P2 | 🔨 | Export all tables to structured files for migration/audit. |
| 334 | Data import (migration) | P2 | 🔨 | Import data from another StoreAssistantPro instance. |
| 335 | Database integrity check | P2 | 🔨 | Run SQLite PRAGMA integrity_check. Report any corruption. |

---

## PHASE 5: ADVANCED BILLING & OPERATIONS (Sprint 20–24)

### Sprint 20: Hold & Recall Bills — SalesView enrichment (12 features)

| # | Feature | P | Status | Description |
|---|---------|---|--------|-------------|
| 336 | Hold current bill | P0 | ✅ | Park active cart for later. Customer needs to check something, new customer walks in. |
| 337 | Recall held bill | P0 | ✅ | Resume a previously parked cart. All items and quantities restored. |
| 338 | Multiple held bills | P0 | ✅ | Hold up to configurable N bills simultaneously (default 5). |
| 339 | Held bill timeout | P2 | 🔨 | Auto-discard held bills after configurable period (e.g., 2 hours). |
| 340 | Held bill indicator | P1 | ✅ | Badge/counter showing number of currently held bills. |
| 341 | Held bill customer tag | P1 | 🔨 | Attach customer name to held bill for identification (e.g., "Rahul's bill"). |
| 342 | Held bill notes | P2 | 🔨 | Add text note to parked bill for context. |
| 343 | Held bill history | P2 | 🔨 | Log of all hold/recall actions for audit. |
| 344 | Quick switch held bills | P1 | 🔨 | Keyboard shortcut (F8/F9) to cycle through held bills. |
| 345 | Held bill auto-save | P0 | ✅ | Persist held bills to database. Survives app restart. |
| 346 | Held bill stale cleanup | P1 | ✅ | On startup, detect and archive abandoned held bills from previous sessions. |
| 347 | Held bill limit per user | P2 | 🔨 | Configurable maximum held bills per cashier. |

### Sprint 21: Quotation / Estimate — QuotationsView (14 features)

| # | Feature | P | Status | Description |
|---|---------|---|--------|-------------|
| 348 | Quotation model | P1 | 🔨 | Entity: Id, QuoteNumber, CustomerId, Date, ValidUntil, Status, Items, Total, Notes. |
| 349 | Quotation creation | P1 | 🔨 | Create estimate with customer, line items, prices, taxes. |
| 350 | Quotation print | P1 | 🔨 | Print formatted quotation document for customer. |
| 351 | Quotation → Sale conversion | P1 | 🔨 | One-click: convert accepted quotation into a billing cart and complete sale. |
| 352 | Quotation status workflow | P1 | 🔨 | Draft → Sent → Accepted → Expired → Rejected. Auto-expire after validity date. |
| 353 | Quotation search | P1 | 🔨 | Search by quote number, customer name, date range. |
| 354 | Quotation paging | P1 | 🔨 | Server-side paging for quotation list. |
| 355 | Quotation validity period | P1 | 🔨 | ValidUntil date. System marks expired quotes automatically. |
| 356 | Quotation revision | P2 | 🔨 | Create revised version with changes. Link revisions together. |
| 357 | Quotation duplicate | P2 | 🔨 | Clone existing quotation as starting point for new one. |
| 358 | Quotation history | P1 | 🔨 | View all past quotations with status and outcomes. |
| 359 | Quotation export (CSV) | P2 | 🔨 | Export quotation data to CSV. |
| 360 | Quotation email | P2 | 🔨 | Email quotation as PDF attachment to customer. |
| 361 | Quotation T&C | P2 | 🔨 | Configurable terms & conditions text printed on quotation document. |

### Sprint 22: Purchase Receive & GRN — GRNView (14 features)

| # | Feature | P | Status | Description |
|---|---------|---|--------|-------------|
| 362 | GRN model | P1 | 🔨 | Entity: Id, GRNNumber, POId (nullable), SupplierId, Date, Items, Status, Notes. |
| 363 | GRN from PO | P1 | 🔨 | Create GRN linked to purchase order. Pre-fills expected items from PO. |
| 364 | GRN without PO | P2 | 🔨 | Direct goods receipt for walk-in purchases without prior PO. |
| 365 | GRN line items | P1 | 🔨 | GRNItem: ProductId, QtyExpected, QtyReceived, QtyRejected, UnitCost. |
| 366 | GRN → stock update | P1 | 🔨 | On GRN confirmation, auto-increase product stock by received quantity. |
| 367 | GRN → cost update | P1 | 🔨 | Update product cost price from GRN unit cost (weighted average or latest). |
| 368 | GRN quality check | P2 | 🔨 | Mark items as Accepted/Rejected during receipt inspection. |
| 369 | GRN search | P1 | 🔨 | Search by GRN number, PO number, supplier name, date. |
| 370 | GRN paging | P1 | 🔨 | Server-side paging for GRN list. |
| 371 | GRN print | P2 | 🔨 | Print GRN document as internal goods receipt record. |
| 372 | GRN history | P1 | 🔨 | View all past goods received notes. |
| 373 | GRN export (CSV) | P2 | 🔨 | Export GRN data to CSV. |
| 374 | Purchase return to supplier | P2 | 🔨 | Return defective/excess goods back to supplier with documentation. |
| 375 | Debit note generation | P2 | 🔨 | Generate debit note document for supplier returns (reduces payable). |

### Sprint 23: Barcode Operations — BarcodeView (12 features)

| # | Feature | P | Status | Description |
|---|---------|---|--------|-------------|
| 376 | Barcode label printing | P1 | 🔨 | Generate and print barcode labels for products. |
| 377 | Barcode label template | P1 | 🔨 | Configurable layout: barcode position, text fields, label dimensions. |
| 378 | Label: product name | P1 | 🔨 | Print product name below/above barcode on label. |
| 379 | Label: sale price | P1 | 🔨 | Print sale price on barcode label. |
| 380 | Label: MRP | P1 | 🔨 | Print MRP on label (Indian legal requirement for packaged goods). |
| 381 | Label: size/color | P2 | 🔨 | Print variant info (Size: L, Color: Black) on label. |
| 382 | Batch barcode printing | P1 | 🔨 | Select multiple products → print N labels each on label sheet. |
| 383 | Barcode scanner input handling | P0 | 🔨 | Detect barcode scanner input (fast keystroke sequence) vs manual typing. |
| 384 | Barcode auto-generate | P2 | 🔨 | Auto-generate EAN-13 barcode if product has none assigned. |
| 385 | Barcode format selection | P2 | 🔨 | Choose barcode symbology: EAN-13, Code128, Code39, QR. |
| 386 | Barcode label paper size | P2 | 🔨 | Configure for label sheet: 65-up A4, continuous roll, custom dimensions. |
| 387 | Barcode product lookup | P1 | 🔨 | Scan barcode → show product details popup (not billing, just info). |

### Sprint 24: Dashboard & Analytics — DashboardView enrichment (23 features)

| # | Feature | P | Status | Description |
|---|---------|---|--------|-------------|
| 388 | Today's sales total | P0 | ✅ | Real-time aggregate of today's completed sales. |
| 389 | Today's return total | P0 | 🔨 | Aggregate of today's returns. |
| 390 | Today's net sales | P0 | 🔨 | Sales − Returns for today. |
| 391 | Today's profit | P1 | 🔨 | Net sales revenue − cost of goods sold for today. |
| 392 | Today's transaction count | P0 | ✅ | Number of completed bills today. |
| 393 | Average bill value | P1 | 🔨 | Total sales / transaction count. Key retail metric. |
| 394 | Top 5 products today | P1 | 🔨 | Best-selling products by quantity today. |
| 395 | Low stock alerts count | P0 | ✅ | Badge showing count of products below min stock level. |
| 396 | Out of stock count | P0 | 🔨 | Badge showing count of products with zero stock. |
| 397 | Pending payments count | P1 | 🔨 | Count of customers with outstanding credit balances. |
| 398 | Monthly sales trend | P1 | 🔨 | Bar/line chart showing daily sales for last 30 days. |
| 399 | Monthly expense trend | P2 | 🔨 | Bar chart showing daily expenses for last 30 days. |
| 400 | Category sales pie chart | P2 | 🔨 | Pie chart breaking down revenue by product category. |
| 401 | Payment method pie chart | P1 | 🔨 | Pie chart: Cash % / UPI % / Card % / Credit %. |
| 402 | Year-over-year comparison | P2 | 🔨 | Compare this month vs same month last year. |
| 403 | Sales target vs actual | P2 | 🔨 | Set monthly target. Dashboard shows progress bar. |
| 404 | Quick action tiles | P0 | ✅ | Clickable tiles: New Sale, Add Product, View Reports, Day End. |
| 405 | Recent sales list | P1 | 🔨 | Last 10 transactions: invoice#, time, amount. |
| 406 | Upcoming tasks | P2 | 🔨 | Pending POs, overdue payments, backup reminders. |
| 407 | Dashboard auto-refresh | P1 | 🔨 | Timer refreshes dashboard data every 60 seconds. |
| 408 | Dashboard widget layout | P2 | 🔨 | Drag-and-drop widget positioning. |
| 409 | Dashboard print | P2 | 🔨 | Print daily summary from dashboard. |
| 410 | Dashboard date selector | P1 | 🔨 | View dashboard metrics for any past date, not just today. |

---

## PHASE 6: POLISH & UX (Sprint 25–28)

### Sprint 25: Keyboard Navigation & Accessibility (15 features)

| # | Feature | P | Status | Description |
|---|---------|---|--------|-------------|
| 411 | Full keyboard nav in billing | P0 | ✅ | Tab through: search → qty → discount → payment → complete. No mouse needed. |
| 412 | F-key shortcuts | P0 | ✅ | F1=Help, F2=New Sale, F3=Search, F5=Pay, F8=Hold, F9=Recall. |
| 413 | Ctrl+key shortcuts | P0 | ✅ | Ctrl+N=New, Ctrl+S=Save, Ctrl+P=Print, Ctrl+E=Edit, Ctrl+D=Duplicate. |
| 414 | Shortcut cheat sheet | P1 | 🔨 | F1 or ? key opens overlay listing all keyboard shortcuts. |
| 415 | Focus visible indicator | P0 | ✅ | Clear focus ring on active element. Styled in DesignSystem.xaml. |
| 416 | Tab order optimization | P0 | ✅ | Logical tab sequence matching left-to-right, top-to-bottom reading order. |
| 417 | Escape to cancel | P0 | ✅ | Escape key on all forms triggers cancel/close action. |
| 418 | Enter to confirm | P0 | ✅ | Enter key submits active form (KeyboardNav.DefaultCommand). |
| 419 | Arrow keys in DataGrid | P0 | ✅ | Navigate DataGrid rows with up/down arrows. Enter to select. |
| 420 | Quick search focus (Ctrl+F) | P1 | 🔨 | Ctrl+F focuses the search box from anywhere on the page. |
| 421 | Screen reader labels | P2 | 🔨 | AutomationProperties.Name on all interactive controls for accessibility. |
| 422 | High contrast mode | P2 | 🔨 | Alternative high-contrast color theme for visually impaired users. |
| 423 | Font size scaling | P2 | 🔨 | Configurable font scale factor (100%/125%/150%) for readability. |
| 424 | Keyboard-only billing | P0 | ✅ | Complete entire sale flow without touching mouse. |
| 425 | Input focus restore | P1 | ✅ | After dialog closes, focus returns to the element that opened it. |

### Sprint 26: Error Handling & Validation (14 features)

| # | Feature | P | Status | Description |
|---|---------|---|--------|-------------|
| 426 | Required field validation | P0 | ✅ | Red border and error message for empty required fields. |
| 427 | Numeric field validation | P0 | ✅ | NumericInput attached behavior prevents non-numeric entry. |
| 428 | Max length enforcement | P0 | ✅ | MaxLength on TextBox prevents exceeding field limits. |
| 429 | Duplicate barcode prevention | P0 | ✅ | Unique index on Barcode. Friendly error on duplicate attempt. |
| 430 | Concurrency conflict handling | P0 | ✅ | DbUpdateConcurrencyException caught → "Modified by another user" message. |
| 431 | Network error handling | P0 | ✅ | Offline mode detection → graceful degradation with queued ops. |
| 432 | Validation summary | P1 | 🔨 | Show all validation errors in one panel instead of one at a time. |
| 433 | Field-level error tooltips | P1 | 🔨 | Tooltip popup on hover over invalid field showing specific error. |
| 434 | Auto-retry on transient errors | P1 | ✅ | EF Core execution strategy with configurable retry count. |
| 435 | Friendly error messages | P0 | ✅ | No stack traces shown to user. All errors formatted as user-readable text. |
| 436 | Error logging to file | P0 | ✅ | FileLoggerProvider logs all exceptions with timestamp and context. |
| 437 | Unhandled exception handler | P0 | ✅ | Global handler in App.xaml.cs prevents crash, logs, shows friendly dialog. |
| 438 | Confirmation dialogs | P0 | ✅ | IDialogService.Confirm() before all destructive operations. |
| 439 | Undo last action | P2 | 🔨 | Undo the last delete operation within current session. Time-limited. |

### Sprint 27: Print & Report Templates (16 features)

| # | Feature | P | Status | Description |
|---|---------|---|--------|-------------|
| 440 | Thermal receipt 58mm | P1 | 🔨 | Narrow 58mm thermal printer format. Condensed layout. |
| 441 | Thermal receipt 80mm | P0 | 🔨 | Standard 80mm thermal printer format. Most common in Indian retail. |
| 442 | A4 tax invoice | P1 | 🔨 | Full-page GST tax invoice. Company letterhead, HSN summary, terms. |
| 443 | A5 half-page invoice | P2 | 🔨 | Compact invoice format for smaller transactions. |
| 444 | Delivery challan | P2 | 🔨 | Goods dispatch document for deliveries (no price, only items + qty). |
| 445 | Barcode label sheet | P1 | 🔨 | Multiple barcode labels per A4 sheet (e.g., Avery 65-up). |
| 446 | Price tag printing | P2 | 🔨 | Product name + sale price + MRP tags for shelf display. |
| 447 | Daily sales report print | P1 | 🔨 | Printable formatted daily sales summary. |
| 448 | Monthly sales report print | P1 | 🔨 | Printable formatted monthly aggregate. |
| 449 | Stock report print | P1 | 🔨 | Current inventory snapshot in printable format. |
| 450 | Customer statement print | P2 | 🔨 | Outstanding balance statement for a customer. |
| 451 | Purchase order print | P2 | 🔨 | Formatted PO document to send to supplier. |
| 452 | Quotation print | P2 | 🔨 | Formatted estimate/quotation for customer. |
| 453 | Credit note print | P2 | 🔨 | Return credit note document. |
| 454 | Print preview for all | P1 | 🔨 | On-screen preview before every print operation. |
| 455 | Print settings | P1 | 🔨 | Configure default printer, paper size, margins. |

### Sprint 28: System Settings & Config (15 features)

| # | Feature | P | Status | Description |
|---|---------|---|--------|-------------|
| 456 | App version display | P0 | ✅ | About screen: version number, build date, copyright. |
| 457 | Theme: Light/Dark | P2 | 🔨 | Toggle between light and dark color themes. |
| 458 | Language: English | P0 | ✅ | Default English UI. Framework for future localization. |
| 459 | Date format config | P1 | 🔨 | dd/MM/yyyy (Indian default). Configurable via settings. |
| 460 | Currency format config | P1 | 🔨 | ₹1,23,456.00 (Indian lakh grouping). Already set via en-IN culture. |
| 461 | Page size config | P2 | 🔨 | User chooses 25/50/100 rows per page in DataGrids. |
| 462 | Auto-logout timer | P2 | 🔨 | Inactivity timeout: return to login screen after N minutes. |
| 463 | Sound effects toggle | P2 | 🔨 | Enable/disable beep on barcode scan, alert sounds. |
| 464 | Startup mode config | P2 | 🔨 | Choose whether app starts in Billing mode or Management mode. |
| 465 | License key / activation | P2 | 🔨 | License validation for commercial distribution. |
| 466 | Update check | P2 | 🔨 | Check for new app versions on startup or manually. |
| 467 | System health monitor | P2 | 🔨 | Display: DB file size, memory usage, app uptime. |
| 468 | Factory reset | P2 | 🔨 | Delete all data and return to first-time setup. Master PIN required. |
| 469 | Feature flags management | P1 | ✅ | Enable/disable features via FeatureToggleService and appsettings.json. |
| 470 | Onboarding wizard | P0 | ✅ | First-time setup: firm details → PINs → confirm. 3-step wizard. |

---

## PART B: EXPANSION FEATURES (478) — Gated

### G1: Hardware Integration (48 features)

| # | Feature | Description |
|---|---------|-------------|
| 471 | USB barcode scanner detection | Auto-detect USB HID barcode scanner on startup. |
| 472 | Continuous scan mode | Scanner stays active for rapid multi-item scanning. |
| 473 | Scan-to-field routing | Scanner input routed to active text field or billing search. |
| 474 | Multi-scanner support | Support multiple scanners on different USB ports. |
| 475 | Scan sound feedback | Audible beep confirmation on successful scan. |
| 476 | Scan error handling | Visual/audio alert when scanned barcode not found in DB. |
| 477 | Scanner config UI | Settings page for scanner configuration. |
| 478 | Wireless scanner support | Bluetooth barcode scanner pairing. |
| 479 | Thermal printer auto-detect | Auto-detect connected thermal printer model. |
| 480 | ESC/POS command generation | Generate ESC/POS byte commands for thermal printers. |
| 481 | Logo printing on receipt | Print store logo bitmap on thermal receipt. |
| 482 | Barcode on receipt | Print barcode of invoice number on receipt. |
| 483 | Cash drawer kick | Send ESC/POS command to open cash drawer on sale complete. |
| 484 | Paper cut command | Send auto-cut command after receipt printing. |
| 485 | Printer status monitoring | Check printer online/offline/paper-out status. |
| 486 | Dual printer support | Two printers: one for receipts, one for labels. |
| 487 | Cash drawer auto-open | Automatically open drawer when cash sale completes. |
| 488 | Cash drawer manual open | Manager PIN → open drawer without a sale (for making change). |
| 489 | Cash drawer status | Detect open/closed state of electronic cash drawer. |
| 490 | Multiple cash drawers | Support multiple registers with separate drawers. |
| 491 | Drawer assignment per register | Map specific drawer to specific workstation. |
| 492 | Drawer open count tracking | Count how many times drawer opened per shift. |
| 493 | Drawer close alert | Alert if drawer left open for > N seconds. |
| 494 | USB/serial drawer support | Support both USB and RS-232 connected drawers. |
| 495 | USB weighing scale integration | Read weight from USB-connected digital scale. |
| 496 | Auto-read weight | Automatically read stable weight and fill quantity field. |
| 497 | Weight-to-quantity mapping | Convert weight to qty based on UOM (e.g., meters of fabric). |
| 498 | Tare support | Zero/tare the scale for packaging weight. |
| 499 | Scale configuration | Settings for scale COM port, baud rate, protocol. |
| 500 | Multi-scale support | Support multiple scales for different counters. |
| 501 | Weight display on screen | Show live weight reading in billing UI. |
| 502 | Weight-based pricing | Price per kg/meter calculated from live weight. |
| 503 | Dedicated label printer | Support for Zebra/TSC thermal label printers. |
| 504 | Zebra ZPL support | Generate ZPL commands for Zebra label printers. |
| 505 | TSC TSPL support | Generate TSPL commands for TSC label printers. |
| 506 | Label template designer | Visual drag-drop label layout designer. |
| 507 | Label auto-feed | Automatic feed to next label after print. |
| 508 | Batch label printing | Print multiple labels in one job. |
| 509 | Label preview | On-screen preview of label layout before printing. |
| 510 | Label printer config | Settings for label printer model, port, dimensions. |
| 511 | Wireless label printing | Print labels over WiFi/Bluetooth to mobile printer. |
| 512 | Customer pole display | Show item name + price on VFD pole display. |
| 513 | LCD customer display | Show items and total on LCD screen facing customer. |
| 514 | Item-by-item display | Update display as each item scanned. |
| 515 | Total display | Show grand total prominently after all items scanned. |
| 516 | Idle message on display | Show store name/promo when no transaction active. |
| 517 | Customer-facing screen | Second monitor showing cart in real-time. |
| 518 | Promotional content on display | Show ads/promotions on customer-facing screen during idle. |

### G2: AI & Smart Features (22 features)

| # | Feature | Description |
|---|---------|-------------|
| 519 | Sales trend prediction | ML model predicting next week/month sales volume. |
| 520 | Seasonal demand forecasting | Identify seasonal patterns (Diwali, wedding season) for stock planning. |
| 521 | Auto reorder suggestions | AI suggests products to reorder based on sales velocity and lead time. |
| 522 | Stock optimization | Recommend optimal stock levels to minimize carrying cost while avoiding stockouts. |
| 523 | Competitor price tracking | Track competitor prices for key products. |
| 524 | Dynamic pricing rules | Auto-adjust prices based on demand, stock level, and competition. |
| 525 | Markdown optimization | Suggest optimal markdown timing and percentage for slow movers. |
| 526 | Price elasticity analysis | Measure how price changes affect sales volume. |
| 527 | Customer purchase patterns | Analyze what products customers buy together (market basket analysis). |
| 528 | Customer churn prediction | Identify customers likely to stop buying based on purchase frequency decline. |
| 529 | Customer segment auto-detection | Automatically group customers by behavior (frequent, high-value, dormant). |
| 530 | Next-purchase prediction | Predict when a customer will buy next and what they'll buy. |
| 531 | Fuzzy product search | Search tolerant of typos: "shirrt" matches "shirt". |
| 532 | Phonetic matching | Search by pronunciation: "Kamiz" matches "Kameez". |
| 533 | Recent/frequent suggestions | Search shows recently/frequently accessed products first. |
| 534 | Barcode OCR from camera | Use webcam to read barcode without dedicated scanner. |
| 535 | Unusual transaction alerts | Flag transactions with abnormal patterns (high discount, late night). |
| 536 | Theft detection patterns | Detect patterns suggesting employee theft (voids, no-sales, sweethearting). |
| 537 | Inventory shrinkage alerts | Alert when stock decreases faster than sales can explain. |
| 538 | Price anomaly detection | Flag products priced significantly different from category average. |
| 539 | Discount abuse detection | Detect patterns of excessive/suspicious discounting by specific users. |
| 540 | Void pattern detection | Alert on unusual void/cancel rates per cashier. |

### G3: Commercial / Licensing (27 features)

| # | Feature | Description |
|---|---------|-------------|
| 541 | License key validation | Validate license key format and authenticity. |
| 542 | Trial period management | 30-day free trial with feature access. |
| 543 | Feature-gated Basic tier | Limited features: billing + basic products only. |
| 544 | Feature-gated Pro tier | Full features: reports, inventory, customers. |
| 545 | Feature-gated Enterprise tier | All features including multi-store, API, advanced analytics. |
| 546 | License renewal | Renewal flow with key entry or online verification. |
| 547 | Offline grace period | License works 7 days without internet for verification. |
| 548 | License transfer | Move license from one machine to another. |
| 549 | Usage analytics collection | Anonymous usage stats for product improvement. |
| 550 | Subscription billing | Monthly/annual subscription management. |
| 551 | Free tier limits | Max 50 products, 100 sales/month, 1 user. |
| 552 | Basic tier features | Up to 500 products, unlimited sales, 3 users. |
| 553 | Pro tier features | Unlimited products, all reports, 10 users, backup. |
| 554 | Enterprise tier features | Multi-store, API, AI features, unlimited users. |
| 555 | Plan comparison page | Side-by-side feature comparison for upgrade decision. |
| 556 | Upgrade flow | In-app upgrade: enter key → unlock features instantly. |
| 557 | Downgrade handling | Graceful feature restriction on downgrade without data loss. |
| 558 | Trial-to-paid conversion | Smooth transition from trial to paid plan. |
| 559 | Custom app branding | Replace "StoreAssistantPro" with custom name. |
| 560 | Custom logo replacement | Use client's logo throughout the app. |
| 561 | Custom color theme | Apply client's brand colors to UI. |
| 562 | Custom app name | Change window title and about screen. |
| 563 | Custom splash screen | Show client's branding on app launch. |
| 564 | Custom about screen | Client's company info in about dialog. |
| 565 | Custom receipt branding | Client's logo and name on receipts. |
| 566 | Custom installer branding | Branded installer for distribution. |
| 567 | Custom onboarding text | Personalized setup wizard text. |

### G4: Localization (23 features)

| # | Feature | Description |
|---|---------|-------------|
| 568 | Hindi UI translation | Complete Hindi translation of all UI strings. |
| 569 | Tamil UI translation | Tamil translation. |
| 570 | Telugu UI translation | Telugu translation. |
| 571 | Kannada UI translation | Kannada translation. |
| 572 | Malayalam UI translation | Malayalam translation. |
| 573 | Bengali UI translation | Bengali translation. |
| 574 | Marathi UI translation | Marathi translation. |
| 575 | Gujarati UI translation | Gujarati translation. |
| 576 | Indian number format | Lakh/crore grouping: 1,23,45,678. |
| 577 | Regional date formats | Support for dd-mm-yyyy, yyyy-mm-dd variants. |
| 578 | Regional calendar support | Vikram Samvat, Saka calendar display. |
| 579 | State-wise tax labels | "CGST/SGST" vs "IGST" based on billing state. |
| 580 | Regional receipt templates | Receipt text in regional language. |
| 581 | Resource files (.resx) | .NET resource file infrastructure for translations. |
| 582 | Runtime language switch | Change language without restarting app. |
| 583 | RTL support placeholder | Infrastructure for right-to-left languages (Urdu). |
| 584 | Translated tooltips | Tooltips in selected language. |
| 585 | Translated validation messages | Error messages in selected language. |
| 586 | Translated reports | Report headers/labels in selected language. |
| 587 | Translated receipts | Receipt text in selected language. |
| 588 | Translated email templates | Email content in selected language. |
| 589 | Translated help content | Help text in selected language. |
| 590 | Language auto-detect | Detect Windows language and suggest matching. |

### G5: Multi-Store (49 features)

| # | Feature | Description |
|---|---------|-------------|
| 591 | Store model | Entity: Id, Name, Address, Phone, GSTIN, IsActive. |
| 592 | Store CRUD | Add/edit/delete stores. |
| 593 | Store switch | Switch active store context without logout. |
| 594 | Store-wise data isolation | Each store's data fully separated. |
| 595 | Store configuration | Per-store settings (receipt, numbering, tax). |
| 596 | Store-wise users | Users assigned to specific stores. |
| 597 | Store address management | Separate address per store. |
| 598 | Store-wise GSTIN | Different GSTIN per store. |
| 599 | Store-wise numbering | Separate invoice sequences per store. |
| 600 | Store hierarchy | Parent company → stores. |
| 601 | Inter-store stock transfer | Move stock from Store A to Store B. |
| 602 | Transfer request/approve | Request-approve workflow for transfers. |
| 603 | Transfer tracking | Track status: Requested → In Transit → Received. |
| 604 | Inter-store pricing | Different prices per store for same product. |
| 605 | Consolidated reports | Combined reports across all stores. |
| 606 | Inter-store returns | Accept return at different store than purchase. |
| 607 | Shared product catalog | Central product master shared across stores. |
| 608 | Store comparison reports | Side-by-side performance comparison. |
| 609 | Store-wise dashboard | Dashboard filtered per store. |
| 610 | Store-wise P&L | Profit & loss per store. |
| 611 | Central admin dashboard | Overview of all stores in one view. |
| 612 | Central product master | Manage products centrally, push to stores. |
| 613 | Central price management | Set prices centrally with store overrides. |
| 614 | Central user management | Manage all users across stores. |
| 615 | Central reports | Cross-store aggregate reporting. |
| 616 | Central settings push | Push settings updates to all stores. |
| 617 | Central backup | Backup all store databases centrally. |
| 618 | Central alert management | View alerts from all stores. |
| 619 | Franchise management | Franchise-specific features and royalty. |
| 620 | Royalty tracking | Calculate franchise royalty payments. |
| 621 | Cloud sync engine | Sync data between stores via cloud. |
| 622 | Conflict resolution | Handle edit conflicts across stores. |
| 623 | Offline-first sync | Work offline, sync when connected. |
| 624 | Sync queue | Queue changes for later sync. |
| 625 | Sync status display | Show sync progress and last sync time. |
| 626 | Sync retry | Automatic retry on sync failure. |
| 627 | Selective sync | Choose which data types to sync. |
| 628 | Bandwidth optimization | Compress sync data for slow connections. |
| 629 | Delta sync | Only sync changed records, not full tables. |
| 630 | Sync log | Detailed log of all sync operations. |
| 631 | Real-time sync | Near-instant sync using WebSocket/SignalR. |
| 632 | Sync scheduling | Configure sync frequency (every 5 min, hourly). |
| 633 | Sync encryption | Encrypt data in transit during sync. |
| 634 | Multi-device sync | Sync between desktop and tablet devices. |
| 635 | Sync health monitor | Dashboard showing sync health per store. |
| 636 | Central database | Cloud-hosted central DB option. |
| 637 | Sync authentication | Secure auth tokens for sync connections. |
| 638 | Sync compression | GZIP compress sync payloads. |
| 639 | Partial sync | Sync specific date ranges or categories only. |

### G6: E-commerce Integration (46 features)

| # | Feature | Description |
|---|---------|-------------|
| 640 | Shopify product push | Push products to Shopify store. |
| 641 | WooCommerce product push | Push products to WooCommerce. |
| 642 | Online order pull | Pull orders from e-commerce platform. |
| 643 | Inventory sync to online | Sync stock levels to online store. |
| 644 | Price sync to online | Push price changes to online store. |
| 645 | Image sync to online | Upload product images to online store. |
| 646 | Variant sync | Push size/color variants to online. |
| 647 | Category mapping | Map POS categories to online categories. |
| 648 | SKU mapping | Map POS SKUs to online platform SKUs. |
| 649 | Bulk product sync | Sync all products in one batch operation. |
| 650 | Sync scheduling | Auto-sync at configurable intervals. |
| 651 | Sync error handling | Log and retry failed sync operations. |
| 652 | Webhook receiver | Receive real-time order notifications from platform. |
| 653 | API rate limiting | Respect platform API rate limits. |
| 654 | Online order list | View all online orders in POS. |
| 655 | Order → sale conversion | Convert online order to POS sale record. |
| 656 | Order fulfillment | Mark order as packed/shipped from POS. |
| 657 | Shipping tracking | Add tracking number to fulfilled orders. |
| 658 | Order status sync | Push status updates back to platform. |
| 659 | Cancellation handling | Process online order cancellations. |
| 660 | Online return processing | Handle returns for online orders. |
| 661 | Online refund processing | Process refunds for online returns. |
| 662 | Multi-channel orders | Unified view of orders from all platforms. |
| 663 | Order priority | Priority-sort: same-day > standard. |
| 664 | Amazon connector | List products on Amazon India. |
| 665 | Flipkart connector | List products on Flipkart. |
| 666 | Meesho connector | List products on Meesho. |
| 667 | Marketplace listing management | Manage listings across marketplaces. |
| 668 | Marketplace order import | Import orders from marketplaces. |
| 669 | Marketplace inventory push | Sync stock to marketplace listings. |
| 670 | Marketplace pricing rules | Different pricing per marketplace. |
| 671 | Commission tracking | Track marketplace commission per sale. |
| 672 | Review management | View and respond to customer reviews. |
| 673 | Marketplace analytics | Sales analytics per marketplace. |
| 674 | Own webstore: product catalog | Public product listing page. |
| 675 | Own webstore: shopping cart | Customer adds items to online cart. |
| 676 | Own webstore: checkout | Customer completes purchase online. |
| 677 | Own webstore: payment gateway | Online payment (Razorpay/PayTM). |
| 678 | Own webstore: order notifications | Email notifications for orders. |
| 679 | Own webstore: customer accounts | Customer login and order history. |
| 680 | Own webstore: wishlist | Save products for later. |
| 681 | Own webstore: search | Product search with filters. |
| 682 | Own webstore: filters | Filter by category, price, size, color. |
| 683 | Own webstore: reviews | Customer product reviews. |
| 684 | Own webstore: SEO | SEO-friendly URLs, meta tags, sitemap. |
| 685 | Own webstore: coupons | Online coupon code support. |

### G7: API & Integration (21 features)

| # | Feature | Description |
|---|---------|-------------|
| 686 | Product REST API | CRUD products via HTTP API. |
| 687 | Sale REST API | Create/read sales via API. |
| 688 | Customer REST API | CRUD customers via API. |
| 689 | Inventory REST API | Read/update stock via API. |
| 690 | API authentication (JWT) | JWT token-based API authentication. |
| 691 | API rate limiting | Throttle API requests per client. |
| 692 | API documentation (Swagger) | Auto-generated Swagger/OpenAPI docs. |
| 693 | Tally export | Export data in Tally-compatible XML format. |
| 694 | QuickBooks sync | Sync sales/expenses to QuickBooks. |
| 695 | Zoho Books sync | Sync to Zoho Books accounting. |
| 696 | Journal entry export | Export as double-entry journal entries. |
| 697 | Ledger mapping | Map POS accounts to accounting ledgers. |
| 698 | Auto-posting | Auto-post transactions to accounting software. |
| 699 | Reconciliation | Reconcile POS data with accounting records. |
| 700 | SMS API integration | Send SMS via Twilio/MSG91 for alerts and OTPs. |
| 701 | Email API integration | Send emails via SendGrid/SMTP for invoices and reports. |
| 702 | WhatsApp Business API | Send messages via WhatsApp Business API. |
| 703 | Push notifications | Send push notifications to mobile companion app. |
| 704 | Webhook outbound | Send webhooks to external systems on events. |
| 705 | Slack/Teams alerts | Send alerts to Slack or Microsoft Teams channels. |
| 706 | Notification templates | Configurable message templates for all communication channels. |

### G8: Mobile Companion (11 features)

| # | Feature | Description |
|---|---------|-------------|
| 707 | Mobile dashboard | View sales summary on phone. |
| 708 | Mobile stock check | Check product stock levels on phone. |
| 709 | Mobile sales summary | View daily/weekly sales on phone. |
| 710 | Mobile barcode scan | Use phone camera as barcode scanner. |
| 711 | Mobile price lookup | Scan/search product → see price and stock. |
| 712 | Mobile low stock alerts | Push notification for low stock items. |
| 713 | Mobile daily report | Daily summary notification/view. |
| 714 | Mobile customer lookup | Search customer by phone on mobile. |
| 715 | Mobile quick sale | Simple billing flow on mobile device. |
| 716 | Mobile offline mode | Work offline, sync when connected. |
| 717 | Mobile push notifications | Receive alerts on mobile. |

### G9: Multi-Country (9 features)

| # | Feature | Description |
|---|---------|-------------|
| 718 | Multi-currency support | Handle multiple currencies with conversion. |
| 719 | Country-specific tax (VAT) | Support VAT for non-Indian countries. |
| 720 | Country address format | Address fields match country format. |
| 721 | Country phone format | Phone number validation per country. |
| 722 | Country date/time format | Date format per country locale. |
| 723 | Tax registration number format | VAT number, ABN, EIN format per country. |
| 724 | Country invoice compliance | Invoice format meeting local regulations. |
| 725 | Currency exchange rates | Configurable exchange rates for multi-currency. |
| 726 | Country-specific reports | Reports formatted per country requirements. |

### G10: Niche Vertical Extensions (61 features)

| # | Feature | Description |
|---|---------|-------------|
| 727 | Measurement capture | Record body measurements for custom tailoring. |
| 728 | Alteration orders | Create alteration work orders (hem, take-in, etc.). |
| 729 | Alteration tracking | Track status: Pending → In Progress → Ready → Delivered. |
| 730 | Alteration delivery date | Set and track promised delivery date. |
| 731 | Fitting notes | Free-text notes per alteration order. |
| 732 | Alteration pricing | Configurable prices per alteration type. |
| 733 | Alteration receipt | Print alteration receipt/token for customer. |
| 734 | Tailor assignment | Assign alteration to specific tailor/worker. |
| 735 | Alteration status board | Dashboard showing all pending alterations. |
| 736 | Alteration history | Past alterations per customer. |
| 737 | Alteration charges on bill | Add alteration charges to sale bill. |
| 738 | Alteration reports | Revenue and productivity from alterations. |
| 739 | Rental product flag | Mark products available for rent. |
| 740 | Rental period definition | Define rental duration (hours, days, weeks). |
| 741 | Rental pricing | Price per day/week for rental items. |
| 742 | Rental return tracking | Track when rented item should be returned. |
| 743 | Rental late fee | Auto-calculate late fee for overdue returns. |
| 744 | Rental agreement print | Print rental agreement document. |
| 745 | Rental deposit management | Collect/refund security deposit. |
| 746 | Rental calendar | Visual calendar showing rental schedule. |
| 747 | Rental availability check | Check if item available for requested dates. |
| 748 | Rental reports | Revenue and utilization from rentals. |
| 749 | Wholesale pricing tiers | Different prices based on order quantity. |
| 750 | Bulk quantity discounts | Auto-discount for bulk purchases. |
| 751 | B2B credit terms | Net 30/60/90 day payment terms. |
| 752 | Customer purchase orders | Accept POs from B2B customers. |
| 753 | Delivery challan generation | Generate challan for goods dispatch. |
| 754 | B2B invoice format | Invoice with buyer GSTIN, HSN summary, terms. |
| 755 | GST B2B compliance | Ensure B2B invoices meet GST requirements. |
| 756 | Trade discounts | Discounts not shown on invoice (off-invoice). |
| 757 | Volume pricing tables | Price lookup table based on quantity ranges. |
| 758 | B2B customer portal | Web portal for B2B customers to view orders/invoices. |
| 759 | Consignment stock tracking | Track stock owned by consignor but held in store. |
| 760 | Consignor management | CRUD for consignment partners. |
| 761 | Commission calculation | Calculate store commission on consignment sales. |
| 762 | Consignment settlement | Periodic settlement reports for consignors. |
| 763 | Unsold consignment return | Return unsold consignment goods to owner. |
| 764 | Consignment reports | Sales and commission per consignor. |
| 765 | Consignment agreement | Print/store consignment agreement document. |
| 766 | Consignment stock aging | Track how long consignment items have been in store. |
| 767 | Consignment invoice | Invoice specific to consignment sales. |
| 768 | Consignment reconciliation | Reconcile consignment stock periodically. |
| 769 | Season model | Entity: Id, Name (Summer 2025), StartDate, EndDate. |
| 770 | Season-wise collections | Tag products to specific seasons. |
| 771 | Markdown calendar | Plan markdown schedules per season. |
| 772 | End-of-season sale | Automated pricing for clearance. |
| 773 | Season comparison reports | Compare performance across seasons. |
| 774 | Seasonal inventory planning | Plan stock purchases per season forecast. |
| 775 | Lookbook management | Create digital lookbooks/catalogs per season. |
| 776 | Trend tracking | Track which styles/colors trending per season. |
| 777 | Season archive | Archive completed season data. |
| 778 | Season P&L | Profit & loss per season. |
| 779 | Gift card issuance | Create gift cards with value and expiry. |
| 780 | Gift card redemption | Apply gift card as payment method. |
| 781 | Gift card balance check | Check remaining balance on gift card. |
| 782 | Gift card expiry | Auto-expire gift cards after validity period. |
| 783 | Loyalty program rules | Configure earning and redemption rules. |
| 784 | Points earning rules | ₹1 = 1 point (configurable ratio). |
| 785 | Points redemption | Redeem points as discount (100 points = ₹10). |
| 786 | Loyalty tier benefits | Tier-specific perks (Gold gets 5% extra). |
| 787 | Loyalty reports | Points earned, redeemed, expired per period. |

### G11: Touch & Kiosk (11 features)

| # | Feature | Description |
|---|---------|-------------|
| 788 | Large touch targets | Minimum 44px touch targets for buttons and controls. |
| 789 | On-screen numpad | Full-size numpad for quantity and PIN entry. |
| 790 | Swipe gestures | Swipe to delete cart item, swipe to navigate. |
| 791 | Pinch-to-zoom on reports | Zoom into report charts and tables. |
| 792 | Touch-friendly DataGrid | Larger row heights, wider tap areas. |
| 793 | Full-screen kiosk mode | Borderless full-screen, no taskbar. |
| 794 | Auto-hide taskbar | Hide Windows taskbar in kiosk mode. |
| 795 | Touch keyboard integration | Show on-screen keyboard for text fields. |
| 796 | Gesture shortcuts | Custom gestures for common actions. |
| 797 | Touch-optimized billing | Redesigned billing UI for touch interaction. |
| 798 | Customer self-checkout kiosk | Customer-facing kiosk for self-service billing. |

### G12: CRM (25 features)

| # | Feature | Description |
|---|---------|-------------|
| 799 | SMS campaigns | Send bulk SMS to customer segments. |
| 800 | Email campaigns | Send bulk email with personalized content. |
| 801 | Birthday auto-greetings | Auto-send birthday wishes via SMS/WhatsApp. |
| 802 | Anniversary offers | Auto-send anniversary discount offers. |
| 803 | Abandoned cart reminders | Remind customers who didn't complete purchase (online). |
| 804 | Feedback collection | Post-purchase feedback via SMS/email link. |
| 805 | NPS surveys | Net Promoter Score survey after purchase. |
| 806 | Customer segmentation | Group by spend, frequency, recency (RFM analysis). |
| 807 | Targeted promotions | Send specific offers to specific segments. |
| 808 | Campaign analytics | Track open rates, conversion, revenue from campaigns. |
| 809 | Complaint tracking | Record and track customer complaints. |
| 810 | Complaint resolution | Track resolution status and notes. |
| 811 | Service requests | Non-complaint service requests (alterations, special orders). |
| 812 | Follow-up reminders | Scheduled follow-up alerts for pending issues. |
| 813 | Customer satisfaction score | Track CSAT per customer interaction. |
| 814 | Escalation rules | Auto-escalate unresolved complaints after N days. |
| 815 | SLA tracking | Track service level agreement compliance. |
| 816 | Service history | Full service interaction history per customer. |
| 817 | Warranty tracking | Track product warranties per customer purchase. |
| 818 | After-sales support | Support ticket system for post-purchase issues. |
| 819 | SMS templates | Pre-built SMS templates with merge fields. |
| 820 | Email templates | HTML email templates with merge fields. |
| 821 | WhatsApp templates | WhatsApp message templates (pre-approved). |
| 822 | Template personalization | Auto-fill customer name, purchase details in templates. |
| 823 | Template scheduling | Schedule messages for future delivery. |

### G13: HR / Staff (8 features)

| # | Feature | Description |
|---|---------|-------------|
| 824 | Staff attendance | Clock in/out with timestamp per employee. |
| 825 | Shift scheduling | Create and manage work shift schedules. |
| 826 | Salary/commission tracking | Track salary + commission per employee. |
| 827 | Leave management | Request, approve, track employee leaves. |
| 828 | Performance metrics | Sales per hour, items per transaction per employee. |
| 829 | Staff targets | Set monthly sales targets per employee. |
| 830 | Overtime tracking | Track hours worked beyond scheduled shift. |
| 831 | Payroll export | Export payroll data for accounting. |

### G14: Compliance (17 features)

| # | Feature | Description |
|---|---------|-------------|
| 832 | GSTR-1 auto-fill | Auto-populate GSTR-1 from sales data. |
| 833 | GSTR-3B summary | Generate GSTR-3B monthly return summary. |
| 834 | GSTR-2A reconciliation | Match purchase invoices with supplier's GSTR-1. |
| 835 | e-Way bill generation | Generate e-Way bill for goods movement > ₹50K. |
| 836 | e-Invoice generation (IRN) | Generate Invoice Reference Number via GST portal API. |
| 837 | QR code mandate | QR code on invoices (mandatory for turnover > ₹500 Cr). |
| 838 | HSN summary report | HSN-wise summary for GST returns. |
| 839 | Tax payment tracker | Track GST payment dates and amounts. |
| 840 | GST annual return data | Generate data for GSTR-9 annual return. |
| 841 | Digital signature on invoices | Apply digital signature to PDF invoices. |
| 842 | Data retention policy | Configurable data retention (7 years for tax records). |
| 843 | Data deletion (GDPR-like) | Delete customer personal data on request. |
| 844 | Consumer protection disclaimers | Display mandatory consumer rights information. |
| 845 | Return policy display | Print return/exchange policy on receipts. |
| 846 | Price display regulations | Ensure MRP and inclusive price displayed as required. |
| 847 | Weight/measure compliance | Legal metrology compliance for weight-based goods. |
| 848 | Anti-profiteering compliance | Track and report benefit of tax reduction to consumers. |

### G15: UI Polish (18 features)

| # | Feature | Description |
|---|---------|-------------|
| 849 | Page transition animations | Smooth fade/slide between pages. |
| 850 | Form reveal animations | Slide-fade animation when forms appear. |
| 851 | Toast notifications | Success/error toast popups with auto-dismiss. |
| 852 | Loading skeletons | Placeholder shapes while data loads. |
| 853 | Progress indicators | Progress bar for long operations (import/export). |
| 854 | Smooth scrolling | Animated scroll in DataGrids and lists. |
| 855 | Card hover effects | Subtle elevation change on card hover. |
| 856 | DataGrid row animations | Fade-in animation for new rows. |
| 857 | Dialog open/close animations | Scale/fade animations for dialogs. |
| 858 | Splash screen animation | Animated logo/progress on app startup. |
| 859 | Icon library | Consistent Fluent/Segoe icon set throughout. |
| 860 | Spacing audit | Ensure consistent spacing across all views. |
| 861 | Responsive layout | Proper behavior on window resize. |
| 862 | High-DPI support | Crisp rendering on 4K displays. |
| 863 | Print-specific styles | Optimized styles for printed output. |
| 864 | Empty state illustrations | Friendly graphics when lists are empty. |
| 865 | Color-coded status badges | Visual badges: green=active, red=inactive, yellow=low stock. |
| 866 | Data visualization charts | Bar, line, pie charts for reports and dashboard. |

### G16: DB Administration (14 features)

| # | Feature | Description |
|---|---------|-------------|
| 867 | DB size monitor | Display current database file size. |
| 868 | DB vacuum/optimize | Run SQLite VACUUM to reclaim space. |
| 869 | Migration manager | Apply/rollback EF Core migrations. |
| 870 | Seed data tool | Load sample data for demos/testing. |
| 871 | Data purge (old records) | Delete records older than N years (with backup). |
| 872 | Archive old sales | Move old sales to archive table for performance. |
| 873 | Index rebuild | Rebuild SQLite indexes for query performance. |
| 874 | Query performance log | Log slow queries for diagnosis. |
| 875 | DB export (JSON) | Export entire database as JSON files. |
| 876 | DB import (JSON) | Import database from JSON files. |
| 877 | Schema version display | Show current DB schema version. |
| 878 | Connection pool monitor | Monitor DB connection pool usage. |
| 879 | Deadlock detection | Detect and log database deadlocks. |
| 880 | Automatic maintenance | Scheduled DB optimization and cleanup. |

### G17: Document Management (9 features)

| # | Feature | Description |
|---|---------|-------------|
| 881 | PDF invoice generation | Generate invoice as PDF file. |
| 882 | PDF receipt generation | Generate receipt as PDF file. |
| 883 | PDF report generation | Generate reports as PDF files. |
| 884 | Document storage | Store generated documents in organized folder structure. |
| 885 | Document search | Search documents by date, type, customer. |
| 886 | Document email | Email documents directly from storage. |
| 887 | Document print queue | Queue multiple documents for batch printing. |
| 888 | Document template editor | Edit document templates (header, footer, layout). |
| 889 | Batch document generation | Generate multiple invoices/reports in one operation. |

### G18: Workflow Automation (8 features)

| # | Feature | Description |
|---|---------|-------------|
| 890 | Low stock → auto PO | Automatically create purchase order when stock falls below minimum. |
| 891 | Day end → auto backup | Trigger automatic backup when day is closed. |
| 892 | Sale complete → auto print | Automatically print receipt on sale completion. |
| 893 | Credit limit → auto alert | Alert when customer approaches/exceeds credit limit. |
| 894 | Inventory → auto reorder | Automated reorder based on sales velocity and lead time. |
| 895 | Expiry → auto markdown | Auto-reduce price as product approaches expiry date. |
| 896 | Milestone → auto upgrade | Auto-promote customer loyalty tier on reaching spend milestone. |
| 897 | Schedule → auto report | Auto-generate and email daily/weekly reports. |

### G19: User Preferences (19 features)

| # | Feature | Description |
|---|---------|-------------|
| 898 | Default landing page | Choose which page opens after login. |
| 899 | Default view mode | List vs grid view preference. |
| 900 | Items per page preference | Personal rows-per-page setting. |
| 901 | Default printer | Personal default printer selection. |
| 902 | Default payment method | Pre-select preferred payment method. |
| 903 | Sidebar state | Remember collapsed/expanded sidebar. |
| 904 | Column visibility | Show/hide DataGrid columns per user. |
| 905 | Sort preference persistence | Remember last sort column/direction. |
| 906 | Filter preference persistence | Remember last applied filters. |
| 907 | Email notification toggle | Enable/disable email notifications. |
| 908 | SMS notification toggle | Enable/disable SMS notifications. |
| 909 | In-app notification toggle | Enable/disable in-app notifications. |
| 910 | Alert sound preference | Choose alert sound or mute. |
| 911 | Low stock alert threshold | Personal threshold for low stock alerts. |
| 912 | Daily report auto-email | Auto-email daily report to self. |
| 913 | Weekly summary preference | Enable/disable weekly summary email. |
| 914 | Notification quiet hours | No notifications during specified hours. |
| 915 | Notification priority filter | Only show high-priority notifications. |
| 916 | Notification history | View past dismissed notifications. |

### G20: Payment Processing (17 features)

| # | Feature | Description |
|---|---------|-------------|
| 917 | Razorpay integration | Accept payments via Razorpay gateway. |
| 918 | PayTM integration | Accept payments via PayTM gateway. |
| 919 | PhonePe integration | Accept payments via PhonePe. |
| 920 | Google Pay integration | Accept payments via Google Pay. |
| 921 | UPI deep link | Generate UPI payment deep link for customer. |
| 922 | Payment status polling | Poll gateway for payment confirmation. |
| 923 | Refund via gateway | Process refund through payment gateway. |
| 924 | Payment reconciliation | Reconcile gateway settlements with POS records. |
| 925 | EMI support | Allow EMI (installment) payment option. |
| 926 | Partial payment tracking | Track payments made in installments. |
| 927 | Payment plan | Create structured payment schedule. |
| 928 | Advance payment / deposit | Accept and track advance deposits. |
| 929 | Payment reminder SMS | Auto-send SMS for overdue payments. |
| 930 | Payment receipt | Generate receipt for payment collection. |
| 931 | Payment history per customer | View all payments from a customer. |
| 932 | Credit/debit note adjustment | Adjust outstanding with credit/debit notes. |
| 933 | Foreign currency acceptance | Accept and convert foreign currency payments. |

### G21: Budgeting & Planning (6 features)

| # | Feature | Description |
|---|---------|-------------|
| 934 | Monthly sales budget | Set target sales amount per month. |
| 935 | Expense budget | Set target expense limit per month/category. |
| 936 | Budget vs actual report | Compare budgeted vs actual sales and expenses. |
| 937 | Variance analysis | Analyze why actuals differ from budget. |
| 938 | Trend-based forecast | Forecast future months based on historical trends. |
| 939 | Financial goal setting | Set and track financial goals (revenue, profit margin). |

### G22: Advanced Reporting (9 features)

| # | Feature | Description |
|---|---------|-------------|
| 940 | Custom report builder | Drag-and-drop report designer with field selection. |
| 941 | Scheduled report emails | Auto-email reports on schedule (daily, weekly, monthly). |
| 942 | Report bookmarks | Save frequently-used report configurations. |
| 943 | Comparative reports | Period vs period comparison (this month vs last). |
| 944 | Drill-down reports | Click aggregate → see underlying detail records. |
| 945 | Report sharing | Share report view with other users. |
| 946 | KPI dashboard | Key Performance Indicators: conversion rate, AOV, sell-through. |
| 947 | Real-time analytics | Live-updating sales and inventory metrics. |
| 948 | Report access control | Role-based access to specific reports. |

---

## Summary

| Section | Range | Count | Description |
|---------|-------|-------|-------------|
| Phase 1 | #1–95 | 95 | Product, Category, Brand, Variant, Inventory, Supplier |
| Phase 2 | #96–190 | 95 | Billing, Payment, Receipt, Returns, Customer, Discount |
| Phase 3 | #191–270 | 80 | GST Tax, Purchase Orders, Expenses, Cash Register, P&L |
| Phase 4 | #271–335 | 65 | Users, Permissions, Audit, Firm Settings, Backup |
| Phase 5 | #336–410 | 75 | Hold Bills, Quotation, GRN, Barcode Ops, Dashboard |
| Phase 6 | #411–470 | 60 | Keyboard, Validation, Print, System Settings |
| G1 | #471–518 | 48 | Hardware: Scanner, Printer, Drawer, Scale, Display |
| G2 | #519–540 | 22 | AI: Forecasting, Pricing, Insights, Anomaly |
| G3 | #541–567 | 27 | Commercial: Licensing, Tiers, White Label |
| G4 | #568–590 | 23 | Localization: 8 Indian Languages, Formats |
| G5 | #591–639 | 49 | Multi-Store: Stores, Transfers, Sync |
| G6 | #640–685 | 46 | E-commerce: Shopify, Amazon, Own Webstore |
| G7 | #686–706 | 21 | API: REST, Accounting, Communication |
| G8 | #707–717 | 11 | Mobile Companion App |
| G9 | #718–726 | 9 | Multi-Country: Currency, Tax, Formats |
| G10 | #727–787 | 61 | Niche: Tailoring, Rental, Wholesale, Consignment, Season, Loyalty |
| G11 | #788–798 | 11 | Touch & Kiosk Mode |
| G12 | #799–823 | 25 | CRM: Campaigns, Service, Templates |
| G13 | #824–831 | 8 | HR: Attendance, Shifts, Payroll |
| G14 | #832–848 | 17 | Compliance: GST Returns, e-Invoice, Legal |
| G15 | #849–866 | 18 | UI Polish: Animations, Icons, Charts |
| G16 | #867–880 | 14 | DB Admin: Optimize, Migrate, Archive |
| G17 | #881–889 | 9 | Documents: PDF, Storage, Templates |
| G18 | #890–897 | 8 | Workflow Automation |
| G19 | #898–916 | 19 | User Preferences |
| G20 | #917–933 | 17 | Payment Gateway Integration |
| G21 | #934–939 | 6 | Budgeting & Planning |
| G22 | #940–948 | 9 | Advanced Reporting |
| **TOTAL** | **#1–948** | **948** | |
