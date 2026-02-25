# StoreAssistantPro — Feature Catalogue (948 Features)

> **Indian Clothing Retail Desktop POS** · .NET 10 · WPF · MVVM
>
> 470 Core Features (6 Phases / 28 Sprints) + 478 Expansion Features (22 Gates)
>
> Status: ✅ Done · ⏭ Skipped · ⏳ Pending

---

## Phase 1 — Foundation & Product Model

### Sprint 1: Product Model Enrichment (Window: Products)

| # | Feature | Status |
|---|---|---|
| 1 | Product: CostPrice field | ✅ |
| 2 | Product: Barcode field | ✅ |
| 3 | Product: UOM field (unit of measure) | ✅ |
| 4 | Product: MRP field (maximum retail price) | ⏭ |
| 5 | Product: SKU field (stock keeping unit) | ⏭ |
| 6 | Product: HSN Code field (tax classification) | ✅ |
| 7 | Product: IsActive toggle (soft delete) | ✅ |
| 8 | Product: IsTaxInclusive flag | ✅ |
| 9 | Product: Color field | ✅ |
| 10 | Product: SalePrice field | ✅ |
| 11 | Product: Quantity field | ✅ |
| 12 | Product: BrandId foreign key | ✅ |
| 13 | Product: MinStockLevel field | ✅ |
| 14 | Product: MaxStockLevel field | ✅ |
| 15 | Product: IsLowStock computed property | ✅ |
| 16 | Product: IsOverStock computed property | ✅ |
| 17 | Product: StockValue computed (CostPrice × Qty) | ✅ |
| 18 | Product: TaxProfileId foreign key | ✅ |
| 19 | Product: RowVersion concurrency token | ✅ |
| 20 | Product: Margin computed (SalePrice − CostPrice) | ✅ |

### Sprint 2: Product Management UX (Window: Products)

| # | Feature | Status |
|---|---|---|
| 21 | Products: Inline Add form | ✅ |
| 22 | Products: Inline Edit form | ✅ |
| 23 | Products: Delete with Master PIN | ✅ |
| 24 | Products: Bulk Delete command | ✅ |
| 25 | Products: Duplicate/Clone product | ✅ |
| 26 | Products: Stock Adjustment form | ✅ |
| 27 | Products: Search by name/barcode/HSN | ✅ |
| 28 | Products: Stock filter (All/Low/Normal/Out) | ✅ |
| 29 | Products: Active filter (All/Active/Inactive) | ✅ |
| 30 | Products: Brand filter ComboBox | ✅ |
| 31 | Products: Color filter ComboBox | ✅ |
| 32 | Products: Tax Profile filter ComboBox | ✅ |
| 33 | Products: UOM filter ComboBox | ✅ |
| 34 | Products: Server-side paging | ✅ |
| 35 | Products: Server-side sorting | ✅ |
| 36 | Products: Import from CSV | ✅ |
| 37 | Products: Export to CSV | ✅ |
| 38 | Products: Export Low Stock to CSV | ✅ |

### Sprint 3: Product DataGrid Enrichment (Window: Products)

| # | Feature | Status |
|---|---|---|
| 39 | Products: ID column | ✅ |
| 40 | Products: Name column (star width) | ✅ |
| 41 | Products: Brand column with tooltip showing product count | ✅ |
| 42 | Products: Color column | ✅ |
| 43 | Products: HSN column | ✅ |
| 44 | Products: Barcode column | ✅ |
| 45 | Products: Sale Price column (currency) | ✅ |
| 46 | Products: Cost Price column (currency) | ✅ |
| 47 | Products: Qty column with low/overstock indicators | ✅ |
| 48 | Products: UOM column | ✅ |
| 49 | Products: Min Stock column | ✅ |
| 50 | Products: Max Stock column | ✅ |
| 51 | Products: Stock Value column (currency) | ✅ |
| 52 | Products: Tax Profile column | ✅ |
| 53 | Products: Status column (Active/Inactive) | ✅ |
| 54 | Products: Margin column (Sale − Cost) | ✅ |
| 55 | Products: Empty state message | ✅ |

### Sprint 4: Brands & Tax (Windows: Brands, Tax)

| # | Feature | Status |
|---|---|---|
| 56 | Brand: Name field | ✅ |
| 57 | Brand: IsActive flag | ✅ |
| 58 | Brand: RowVersion concurrency token | ✅ |
| 59 | Brand: Products navigation collection | ✅ |
| 60 | Brand: ProductCount computed property | ✅ |
| 61 | Brands: Inline Add form | ✅ |
| 62 | Brands: Inline Edit form | ✅ |
| 63 | Brands: Delete brand | ✅ |
| 64 | Brands: Search by name | ✅ |
| 65 | Brands: DataGrid with columns | ✅ |
| 66 | Brands: Empty state message | ✅ |
| 67 | Tax: TaxProfile model | ✅ |
| 68 | Tax: ProfileName field | ✅ |
| 69 | Tax: CGST rate field | ✅ |
| 70 | Tax: SGST rate field | ✅ |
| 71 | Tax: IsDefault flag | ✅ |
| 72 | Tax: Management view with CRUD | ✅ |

---

## Phase 2 — Sales & Billing

### Sprint 5: Sale Model & Core (Window: Sales)

| # | Feature | Status |
|---|---|---|
| 73 | Sale: Id primary key | ✅ |
| 74 | Sale: InvoiceNumber generation | ✅ |
| 75 | Sale: SaleDate timestamp | ✅ |
| 76 | Sale: TotalAmount field | ✅ |
| 77 | Sale: IdempotencyKey (duplicate prevention) | ✅ |
| 78 | Sale: PaymentMethod field | ✅ |
| 79 | Sale: CashierRole audit field | ✅ |
| 80 | Sale: DiscountType enum field | ✅ |
| 81 | Sale: DiscountValue field | ✅ |
| 82 | Sale: DiscountAmount computed field | ✅ |
| 83 | Sale: DiscountReason field | ✅ |
| 84 | Sale: PaymentReference field | ✅ |
| 85 | Sale: RowVersion concurrency token | ✅ |
| 86 | Sale: Items navigation collection | ✅ |
| 87 | Sale: ItemsSummary computed property | ✅ |
| 88 | SaleItem: ProductId foreign key | ✅ |
| 89 | SaleItem: Quantity field | ✅ |
| 90 | SaleItem: UnitPrice field | ✅ |

### Sprint 6: Cart & Checkout (Window: Sales)

| # | Feature | Status |
|---|---|---|
| 91 | SaleItem: Subtotal computed (Qty × Price) | ✅ |
| 92 | Sales: New Sale toggle form | ✅ |
| 93 | Sales: Product picker ComboBox | ✅ |
| 94 | Sales: Cart quantity input | ✅ |
| 95 | Sales: Add to Cart command | ✅ |
| 96 | Sales: Remove from Cart command | ✅ |
| 97 | Sales: Cart DataGrid (Product/Price/Qty/Subtotal) | ✅ |
| 98 | Sales: Cart total calculation | ✅ |
| 99 | Sales: Duplicate cart item merge (quantity add) | ✅ |
| 100 | Sales: Stock availability check on add | ✅ |
| 101 | Sales: Payment method selector | ✅ |
| 102 | Sales: Complete Sale command with save | ✅ |
| 103 | Sales: Cancel New Sale command | ✅ |
| 104 | Sales: Processing overlay during save | ✅ |
| 105 | Sales: Saving status text feedback | ✅ |
| 106 | Sales: Cart lock during payment | ✅ |
| 107 | Sales: Error message display | ✅ |
| 108 | Sales: Cart remove button (✕) per row | ✅ |

### Sprint 7: Sales History & Detail (Window: Sales)

| # | Feature | Status |
|---|---|---|
| 109 | Sales: History DataGrid with columns | ✅ |
| 110 | Dashboard: Top Selling Products Panel | ✅ |
| 111 | Sales: Export Filtered Sales to CSV | ✅ |
| 112 | Sales: Date column with full-date tooltip | ✅ |
| 113 | Sales: Total column (currency) | ✅ |
| 114 | Sales: Payment method column | ✅ |
| 115 | Sales: Cashier column | ✅ |
| 116 | Sales: Items column with summary tooltip | ✅ |
| 117 | Sales: Sale Detail Panel (invoice header) | ✅ |
| 118 | Sales: Detail Panel — item DataGrid | ✅ |
| 119 | Sales: Print Invoice stub command | ✅ |
| 120 | Sales: Payment Reference tooltip on Ref # column | ✅ |
| 121 | Sales: Refund Sale stub command | ✅ |
| 122 | Sales: Discount column in history DataGrid | ✅ |
| 123 | Sales: Export Sale Items command | ✅ |
| 124 | Sales: Reference row in Detail Panel | ✅ |
| 125 | Sales: Empty state message | ✅ |

### Sprint 8: Sales Filtering & Paging (Window: Sales)

| # | Feature | Status |
|---|---|---|
| 126 | Sales: Search by invoice/payment/cashier | ✅ |
| 127 | Sales: Filter by date range (From/To) | ✅ |
| 128 | Sales: Load All (clear filters) | ✅ |
| 129 | Sales: Payment method filter ComboBox | ✅ |
| 130 | Sales: Sort indicator arrows on column headers | ✅ |
| 131 | Sales: Cashier filter ComboBox | ✅ |
| 132 | Sales: Server-side paging | ✅ |
| 133 | Sales: Page size selector | ✅ |
| 134 | Sales: Previous/Next page commands | ✅ |
| 135 | Sales: Page display text (Page X of Y) | ✅ |
| 136 | Sales: Server-side column sorting | ✅ |
| 137 | Sales: Export All Sales to CSV | ✅ |
| 138 | Sales: Reprint Last Invoice stub | ✅ |
| 139 | Sales: Export All Sale Items command | ✅ |
| 140 | Sales: Keyboard shortcut Ctrl+N (New Sale) | ✅ |
| 141 | Sales: Keyboard shortcut Ctrl+Shift+E (Export) | ✅ |
| 142 | Sales: Escape to cancel new sale form | ✅ |

### Sprint 9: Discounts & Bill Calculation (Window: Sales)

| # | Feature | Status |
|---|---|---|
| 143 | Sales: Discount type selector (None/Percentage/Amount) | ✅ |
| 144 | Sales: Discount value input | ✅ |
| 145 | Sales: Discount reason input | ✅ |
| 146 | Sales: Discount amount display (−₹X) | ✅ |
| 147 | Sales: Bill final amount with discount | ✅ |
| 148 | Sales: Subtotal display when discount active | ✅ |
| 149 | Sales: Percentage discount cap (max 100%) | ✅ |
| 150 | Sales: Amount discount cap (max subtotal) | ✅ |
| 151 | Sales: BillCalculationService | ✅ |
| 152 | Sales: CompleteSaleHandler (command pipeline) | ✅ |
| 153 | Sales: Invoice number auto-generation | ✅ |
| 154 | Sales: Stock decrement on sale | ✅ |
| 155 | Sales: Idempotency key duplicate check | ✅ |
| 156 | Sales: Transaction safety on sale save | ✅ |
| 157 | Sales: Discount info in Detail Panel | ✅ |
| 158 | Sales: Discount reason tooltip on Discount column | ✅ |
| 159 | Sales: Total record count display near paging | ✅ |

---

## Phase 3 — Inventory & Stock Management

### Sprint 10: Dashboard KPIs (Window: Dashboard)

| # | Feature | Status |
|---|---|---|
| 160 | Dashboard: Total Products KPI card | ✅ |
| 161 | Dashboard: Low Stock Count KPI card | ✅ |
| 162 | Dashboard: Out of Stock Count KPI card | ✅ |
| 163 | Dashboard: Overstock Count KPI card | ✅ |
| 164 | Dashboard: Inventory Value (at cost) KPI card | ✅ |
| 165 | Dashboard: Inventory Value (at sale price) KPI card | ✅ |
| 166 | Dashboard: Today's Sales KPI card | ✅ |
| 167 | Dashboard: Today's Transactions KPI card | ✅ |
| 168 | Dashboard: Average Sale KPI card | ✅ |
| 169 | Dashboard: Today's Total Discount KPI | ✅ |
| 170 | Dashboard: Recent Sales panel | ✅ |
| 171 | Dashboard: Low Stock Products panel | ✅ |
| 172 | Dashboard: Low Stock by Brand panel | ✅ |
| 173 | Dashboard: Out of Stock Products panel | ✅ |
| 174 | Dashboard: Inventory Value by Brand panel | ✅ |
| 175 | Dashboard: Sales by Payment Method panel | ✅ |
| 176 | Dashboard: Top Selling Products panel | ✅ |

### Sprint 11: Stock Management (Window: Products)

| # | Feature | Status |
|---|---|---|
| 177 | Products: Margin percentage column | ✅ |
| 178 | Products: Retail Value column (SalePrice × Qty) | ✅ |
| 179 | Products: Tax Inclusive badge column | ✅ |
| 180 | Products: Barcode tooltip with full value | ✅ |
| 181 | Products: HSN tooltip with full value | ✅ |
| 182 | Products: Row highlight for low stock | ✅ |
| 183 | Products: Row highlight for out of stock | ✅ |
| 184 | Products: Row highlight for inactive | ✅ |
| 185 | Products: Bulk stock adjustment | ⏳ |
| 186 | Products: Stock adjustment history log | ⏳ |
| 187 | Products: Stock adjustment reason codes | ⏳ |
| 188 | Products: Reorder level auto-suggestion | ⏳ |
| 189 | Products: Batch stock import from CSV | ⏳ |
| 190 | Products: Stock snapshot export | ⏳ |
| 191 | Products: Product detail panel (read-only summary) | ✅ |
| 192 | Products: Quick price update inline | ⏳ |
| 193 | Products: Duplicate barcode validation | ✅ |

### Sprint 12: Purchase Orders (Window: Purchase Orders — NEW)

| # | Feature | Status |
|---|---|---|
| 194 | PurchaseOrder model: Id, OrderNumber, Date | ⏳ |
| 195 | PurchaseOrder: SupplierId foreign key | ⏳ |
| 196 | PurchaseOrder: Status enum (Draft/Ordered/Received/Cancelled) | ⏳ |
| 197 | PurchaseOrder: TotalAmount computed | ⏳ |
| 198 | PurchaseOrder: Notes field | ⏳ |
| 199 | PurchaseOrderItem: ProductId, Quantity, UnitCost | ⏳ |
| 200 | Purchase Orders: List view with DataGrid | ⏳ |
| 201 | Purchase Orders: Create new order form | ⏳ |
| 202 | Purchase Orders: Add items to order | ⏳ |
| 203 | Purchase Orders: Remove items from order | ⏳ |
| 204 | Purchase Orders: Submit order (Draft → Ordered) | ⏳ |
| 205 | Purchase Orders: Receive order (stock increment) | ⏳ |
| 206 | Purchase Orders: Cancel order | ⏳ |
| 207 | Purchase Orders: Search by order number | ⏳ |
| 208 | Purchase Orders: Filter by status | ⏳ |
| 209 | Purchase Orders: Filter by supplier | ⏳ |
| 210 | Purchase Orders: Export to CSV | ⏳ |

### Sprint 13: Supplier Management (Window: Suppliers — NEW)

| # | Feature | Status |
|---|---|---|
| 211 | Supplier model: Id, Name, ContactPerson | ⏳ |
| 212 | Supplier: Phone field | ⏳ |
| 213 | Supplier: Email field | ⏳ |
| 214 | Supplier: Address field | ⏳ |
| 215 | Supplier: GSTIN field | ⏳ |
| 216 | Supplier: IsActive flag | ⏳ |
| 217 | Supplier: Notes field | ⏳ |
| 218 | Suppliers: List view with DataGrid | ⏳ |
| 219 | Suppliers: Inline Add form | ⏳ |
| 220 | Suppliers: Inline Edit form | ⏳ |
| 221 | Suppliers: Delete supplier | ⏳ |
| 222 | Suppliers: Search by name/phone/GSTIN | ⏳ |
| 223 | Suppliers: Active filter | ⏳ |
| 224 | Suppliers: Export to CSV | ⏳ |
| 225 | Suppliers: Paging controls | ⏳ |
| 226 | Suppliers: Empty state message | ⏳ |
| 227 | Suppliers: Supplier detail panel | ⏳ |

### Sprint 14: Stock Alerts & Reorder (Window: Products, Dashboard)

| # | Feature | Status |
|---|---|---|
| 228 | Products: Low stock notification badge in sidebar | ⏳ |
| 229 | Products: Auto-reorder suggestion list | ⏳ |
| 230 | Products: Reorder point calculation | ⏳ |
| 231 | Products: Stock aging indicator (days since last sale) | ⏳ |
| 232 | Products: Dead stock identification | ⏳ |
| 233 | Products: Stock turnover rate display | ⏳ |
| 234 | Products: Min/Max stock validation on save | ⏳ |
| 235 | Dashboard: Low stock alert banner | ⏳ |
| 236 | Dashboard: Reorder suggestions panel | ⏳ |
| 237 | Dashboard: Stock health summary (healthy/warning/critical) | ⏳ |
| 238 | Products: Export reorder list to CSV | ⏳ |
| 239 | Products: Bulk update min stock levels | ⏳ |
| 240 | Products: Bulk update max stock levels | ⏳ |
| 241 | Products: Stock movement history per product | ⏳ |
| 242 | Products: Last purchase date display | ⏳ |
| 243 | Products: Last sale date display | ⏳ |
| 244 | Products: Average daily sales rate | ⏳ |

---

## Phase 4 — Customer & Relationships

### Sprint 15: Customer Management (Window: Customers — NEW)

| # | Feature | Status |
|---|---|---|
| 245 | Customer model: Id, Name, Phone | ⏳ |
| 246 | Customer: Email field | ⏳ |
| 247 | Customer: Address field | ⏳ |
| 248 | Customer: GSTIN field (for B2B) | ⏳ |
| 249 | Customer: DateJoined timestamp | ⏳ |
| 250 | Customer: IsActive flag | ⏳ |
| 251 | Customer: Notes field | ⏳ |
| 252 | Customer: TotalPurchases computed | ⏳ |
| 253 | Customers: List view with DataGrid | ⏳ |
| 254 | Customers: Inline Add form | ⏳ |
| 255 | Customers: Inline Edit form | ⏳ |
| 256 | Customers: Delete customer | ⏳ |
| 257 | Customers: Search by name/phone/email | ⏳ |
| 258 | Customers: Active filter | ⏳ |
| 259 | Customers: Paging controls | ⏳ |
| 260 | Customers: Export to CSV | ⏳ |
| 261 | Customers: Empty state message | ⏳ |

### Sprint 16: Customer Loyalty (Window: Customers, Sales)

| # | Feature | Status |
|---|---|---|
| 262 | Customer: LoyaltyPoints field | ⏳ |
| 263 | Customer: LoyaltyTier computed (Bronze/Silver/Gold/Platinum) | ⏳ |
| 264 | Customer: Purchase history panel | ⏳ |
| 265 | Customer: Total spend display | ⏳ |
| 266 | Customer: Average order value display | ⏳ |
| 267 | Customer: Last visit date | ⏳ |
| 268 | Customer: Visit frequency | ⏳ |
| 269 | Sales: Assign customer to sale | ⏳ |
| 270 | Sales: Customer search/picker in new sale form | ⏳ |
| 271 | Sales: Walk-in customer default | ⏳ |
| 272 | Sales: Customer name on invoice | ⏳ |
| 273 | Sales: Loyalty points earn on sale | ⏳ |
| 274 | Sales: Loyalty points redeem on sale | ⏳ |
| 275 | Customers: Loyalty tier badge in DataGrid | ⏳ |
| 276 | Customers: Points balance column | ⏳ |
| 277 | Customers: Birthday field | ⏳ |
| 278 | Customers: Anniversary field | ⏳ |

### Sprint 17: Returns & Refunds (Window: Returns — NEW)

| # | Feature | Status |
|---|---|---|
| 279 | Return model: Id, ReturnNumber, Date | ⏳ |
| 280 | Return: OriginalSaleId reference | ⏳ |
| 281 | Return: Reason field | ⏳ |
| 282 | Return: RefundAmount field | ⏳ |
| 283 | Return: RefundMethod (Cash/Credit/Exchange) | ⏳ |
| 284 | Return: Status enum (Pending/Approved/Completed) | ⏳ |
| 285 | ReturnItem: ProductId, Quantity, UnitPrice | ⏳ |
| 286 | Returns: List view with DataGrid | ⏳ |
| 287 | Returns: Create return from sale | ⏳ |
| 288 | Returns: Select items to return | ⏳ |
| 289 | Returns: Quantity validation (≤ sold qty) | ⏳ |
| 290 | Returns: Stock increment on return | ⏳ |
| 291 | Returns: Approve/complete workflow | ⏳ |
| 292 | Returns: Search by return number | ⏳ |
| 293 | Returns: Filter by date range | ⏳ |
| 294 | Returns: Export to CSV | ⏳ |
| 295 | Returns: Return detail panel | ⏳ |

### Sprint 18: Expense Tracking (Window: Expenses — NEW)

| # | Feature | Status |
|---|---|---|
| 296 | Expense model: Id, Date, Amount, Category | ⏳ |
| 297 | Expense: Description field | ⏳ |
| 298 | Expense: PaymentMethod field | ⏳ |
| 299 | Expense: ReceiptReference field | ⏳ |
| 300 | Expense: Category enum (Rent/Salary/Utilities/Stock/Marketing/Other) | ⏳ |
| 301 | Expenses: List view with DataGrid | ⏳ |
| 302 | Expenses: Inline Add form | ⏳ |
| 303 | Expenses: Inline Edit form | ⏳ |
| 304 | Expenses: Delete expense | ⏳ |
| 305 | Expenses: Search by description | ⏳ |
| 306 | Expenses: Filter by category | ⏳ |
| 307 | Expenses: Filter by date range | ⏳ |
| 308 | Expenses: Monthly total summary | ⏳ |
| 309 | Expenses: Export to CSV | ⏳ |
| 310 | Expenses: Paging controls | ⏳ |
| 311 | Expenses: Empty state message | ⏳ |

### Sprint 19: Promotions & Offers (Window: Promotions — NEW)

| # | Feature | Status |
|---|---|---|
| 312 | Promotion model: Id, Name, StartDate, EndDate | ⏳ |
| 313 | Promotion: DiscountType (Percentage/Flat/BuyXGetY) | ⏳ |
| 314 | Promotion: DiscountValue field | ⏳ |
| 315 | Promotion: MinPurchaseAmount field | ⏳ |
| 316 | Promotion: ApplicableProducts (all or specific) | ⏳ |
| 317 | Promotion: ApplicableBrands filter | ⏳ |
| 318 | Promotion: IsActive computed (date-based) | ⏳ |
| 319 | Promotions: List view with DataGrid | ⏳ |
| 320 | Promotions: Create promotion form | ⏳ |
| 321 | Promotions: Edit promotion form | ⏳ |
| 322 | Promotions: Delete promotion | ⏳ |
| 323 | Promotions: Active/expired filter | ⏳ |
| 324 | Sales: Auto-apply active promotions | ⏳ |
| 325 | Sales: Promotion badge on applicable products | ⏳ |
| 326 | Sales: Promotion discount line on bill | ⏳ |
| 327 | Promotions: Export to CSV | ⏳ |

---

## Phase 5 — Reports & Analytics

### Sprint 20: Sales Reports (Window: Reports — NEW)

| # | Feature | Status |
|---|---|---|
| 328 | Reports: Sales by date range | ⏳ |
| 329 | Reports: Sales by payment method | ⏳ |
| 330 | Reports: Sales by cashier | ⏳ |
| 331 | Reports: Sales by product | ⏳ |
| 332 | Reports: Sales by brand | ⏳ |
| 333 | Reports: Sales by customer | ⏳ |
| 334 | Reports: Daily sales summary | ⏳ |
| 335 | Reports: Weekly sales summary | ⏳ |
| 336 | Reports: Monthly sales summary | ⏳ |
| 337 | Reports: Sales growth comparison (vs previous period) | ⏳ |
| 338 | Reports: Top 10 selling products | ⏳ |
| 339 | Reports: Bottom 10 selling products | ⏳ |
| 340 | Reports: Average basket size | ⏳ |
| 341 | Reports: Discount utilization report | ⏳ |
| 342 | Reports: Hourly sales heatmap data | ⏳ |
| 343 | Reports: Sales trend line data | ⏳ |
| 344 | Reports: Export any report to CSV | ⏳ |

### Sprint 21: Inventory Reports (Window: Reports)

| # | Feature | Status |
|---|---|---|
| 345 | Reports: Current stock valuation (at cost) | ⏳ |
| 346 | Reports: Current stock valuation (at sale price) | ⏳ |
| 347 | Reports: Low stock report | ⏳ |
| 348 | Reports: Out of stock report | ⏳ |
| 349 | Reports: Overstock report | ⏳ |
| 350 | Reports: Dead stock report (no sales in X days) | ⏳ |
| 351 | Reports: Stock movement report (in/out/adjust) | ⏳ |
| 352 | Reports: Stock by brand | ⏳ |
| 353 | Reports: Stock by category/UOM | ⏳ |
| 354 | Reports: Stock aging report | ⏳ |
| 355 | Reports: Stock turnover report | ⏳ |
| 356 | Reports: Purchase order summary | ⏳ |
| 357 | Reports: Supplier-wise purchase report | ⏳ |
| 358 | Reports: Stock adjustment audit report | ⏳ |
| 359 | Reports: Reorder report | ⏳ |
| 360 | Reports: Product profitability report | ⏳ |
| 361 | Reports: Brand-wise profitability report | ⏳ |

### Sprint 22: Financial Reports (Window: Reports)

| # | Feature | Status |
|---|---|---|
| 362 | Reports: Daily cash summary | ⏳ |
| 363 | Reports: Revenue vs expense | ⏳ |
| 364 | Reports: Gross profit report | ⏳ |
| 365 | Reports: Net profit report (revenue − expense − COGS) | ⏳ |
| 366 | Reports: Payment method breakdown | ⏳ |
| 367 | Reports: GST collection report (CGST + SGST) | ⏳ |
| 368 | Reports: Tax liability summary | ⏳ |
| 369 | Reports: Discount given report | ⏳ |
| 370 | Reports: Refund report | ⏳ |
| 371 | Reports: Customer outstanding/dues | ⏳ |
| 372 | Reports: Supplier payment due | ⏳ |
| 373 | Reports: Monthly P&L statement | ⏳ |
| 374 | Reports: Yearly comparison | ⏳ |
| 375 | Reports: Cash flow summary | ⏳ |
| 376 | Reports: Expense by category | ⏳ |
| 377 | Reports: Day-end register report | ⏳ |

### Sprint 23: Dashboard Analytics (Window: Dashboard)

| # | Feature | Status |
|---|---|---|
| 378 | Dashboard: Sales trend sparkline (7-day) | ⏳ |
| 379 | Dashboard: Revenue vs last week comparison | ⏳ |
| 380 | Dashboard: Top brand by revenue | ⏳ |
| 381 | Dashboard: Customer count (today) | ⏳ |
| 382 | Dashboard: New customers (this month) | ⏳ |
| 383 | Dashboard: Return rate percentage | ⏳ |
| 384 | Dashboard: Gross margin percentage | ⏳ |
| 385 | Dashboard: Active promotions count | ⏳ |
| 386 | Dashboard: Pending purchase orders count | ⏳ |
| 387 | Dashboard: Expense today | ⏳ |
| 388 | Dashboard: Net revenue today (sales − returns − discounts) | ⏳ |
| 389 | Dashboard: Stock health pie (healthy/warning/critical) | ⏳ |
| 390 | Dashboard: Quick actions panel | ⏳ |
| 391 | Dashboard: Refresh all stats command | ✅ |
| 392 | Dashboard: Auto-refresh on navigation | ✅ |
| 393 | Dashboard: Loading indicator | ✅ |

### Sprint 24: Export & Print Infrastructure (Cross-cutting)

| # | Feature | Status |
|---|---|---|
| 394 | Export: CSV encoding (UTF-8 with BOM) | ✅ |
| 395 | Export: CSV field escaping (commas/quotes) | ✅ |
| 396 | Export: SaveFileDialog integration | ✅ |
| 397 | Export: Success message with filename | ✅ |
| 398 | Export: Error handling on write failure | ✅ |
| 399 | Export: Products full export | ✅ |
| 400 | Export: Sales full export | ✅ |
| 401 | Export: Filtered sales export | ✅ |
| 402 | Export: Sale items export | ✅ |
| 403 | Export: Low stock export | ✅ |
| 404 | Print: Invoice layout stub | ✅ |
| 405 | Print: Reprint last invoice stub | ✅ |
| 406 | Reports: Print report stub | ⏳ |
| 407 | Reports: PDF export stub | ⏳ |
| 408 | Export: All sale items (cross-sale) | ✅ |
| 409 | Export: Column header row in all CSVs | ✅ |

---

## Phase 6 — Settings & Administration

### Sprint 25: User Management (Window: User Management — NEW)

| # | Feature | Status |
|---|---|---|
| 410 | UserType enum: Admin, Manager, User | ✅ |
| 411 | Login: PIN-based authentication | ✅ |
| 412 | Login: Master PIN for admin | ✅ |
| 413 | Session: Current user tracking | ✅ |
| 414 | Session: Role-based visibility | ✅ |
| 415 | Users: User list view | ⏳ |
| 416 | Users: Create user form | ⏳ |
| 417 | Users: Edit user form | ⏳ |
| 418 | Users: Deactivate user | ⏳ |
| 419 | Users: Reset PIN | ⏳ |
| 420 | Users: Role assignment | ⏳ |
| 421 | Users: Last login display | ⏳ |
| 422 | Users: Activity log per user | ⏳ |
| 423 | Products: CanManageProducts role gate | ✅ |
| 424 | Products: CanDeleteProducts role gate | ✅ |
| 425 | Sales: CanCreateSales role gate | ✅ |

### Sprint 26: App Settings (Window: Settings — NEW)

| # | Feature | Status |
|---|---|---|
| 426 | Settings: Store name configuration | ⏳ |
| 427 | Settings: Store address | ⏳ |
| 428 | Settings: Store phone | ⏳ |
| 429 | Settings: Store GSTIN | ⏳ |
| 430 | Settings: Store logo path | ⏳ |
| 431 | Settings: Invoice prefix configuration | ⏳ |
| 432 | Settings: Invoice number reset policy | ⏳ |
| 433 | Settings: Default payment method | ⏳ |
| 434 | Settings: Default tax profile | ⏳ |
| 435 | Settings: Low stock threshold global default | ⏳ |
| 436 | Settings: Currency display (₹) | ✅ |
| 437 | Settings: Date format (dd/MM/yyyy) | ✅ |
| 438 | Settings: Culture (en-IN) | ✅ |
| 439 | Settings: Timezone (IST) | ✅ |
| 440 | Settings: Theme preference | ⏳ |
| 441 | Settings: Sidebar collapse state persistence | ⏳ |

### Sprint 27: Audit & Security (Window: Audit Log — NEW)

| # | Feature | Status |
|---|---|---|
| 442 | AuditLog model: Id, Timestamp, UserId, Action, Entity | ⏳ |
| 443 | AuditLog: OldValue / NewValue fields | ⏳ |
| 444 | Audit: Log product create | ⏳ |
| 445 | Audit: Log product edit | ⏳ |
| 446 | Audit: Log product delete | ⏳ |
| 447 | Audit: Log sale complete | ⏳ |
| 448 | Audit: Log return/refund | ⏳ |
| 449 | Audit: Log stock adjustment | ⏳ |
| 450 | Audit: Log user login/logout | ⏳ |
| 451 | Audit: Log settings change | ⏳ |
| 452 | Audit Log: List view with DataGrid | ⏳ |
| 453 | Audit Log: Filter by action type | ⏳ |
| 454 | Audit Log: Filter by user | ⏳ |
| 455 | Audit Log: Filter by date range | ⏳ |
| 456 | Audit Log: Export to CSV | ⏳ |

### Sprint 28: Backup, Maintenance & Polish (Cross-cutting)

| # | Feature | Status |
|---|---|---|
| 457 | Database: SQLite backup to file | ⏳ |
| 458 | Database: Restore from backup | ⏳ |
| 459 | Database: Auto-backup on app close | ⏳ |
| 460 | Database: Backup schedule configuration | ⏳ |
| 461 | App: Startup data integrity check | ⏳ |
| 462 | App: Stale billing session cleanup | ✅ |
| 463 | App: Connectivity monitor service | ✅ |
| 464 | App: Offline mode service | ✅ |
| 465 | App: Feature toggle service | ✅ |
| 466 | App: Window sizing service | ✅ |
| 467 | App: Focus lock service | ✅ |
| 468 | App: Context help service | ✅ |
| 469 | App: Tip rotation service | ✅ |
| 470 | App: Onboarding journey service | ✅ |

---

## Expansion Features (Gates G1–G22)

### G1: Hardware Integration (48 features)

| # | Feature | Status |
|---|---|---|
| 471 | Barcode scanner: USB HID input support | ⏳ |
| 472 | Barcode scanner: Auto-detect scan vs keyboard | ⏳ |
| 473 | Barcode scanner: Scan-to-add in cart | ⏳ |
| 474 | Barcode scanner: Scan-to-search in products | ⏳ |
| 475 | Barcode scanner: Scan-to-find in stock adjust | ⏳ |
| 476 | Barcode printer: Label layout designer | ⏳ |
| 477 | Barcode printer: Print single label | ⏳ |
| 478 | Barcode printer: Batch print labels | ⏳ |
| 479 | Barcode printer: QR code generation | ⏳ |
| 480 | Barcode printer: Price tag format | ⏳ |
| 481 | Receipt printer: Thermal printer driver | ⏳ |
| 482 | Receipt printer: 58mm format | ⏳ |
| 483 | Receipt printer: 80mm format | ⏳ |
| 484 | Receipt printer: Auto-print on sale | ⏳ |
| 485 | Receipt printer: Logo on receipt | ⏳ |
| 486 | Receipt printer: Receipt footer text | ⏳ |
| 487 | Receipt printer: GST details on receipt | ⏳ |
| 488 | Receipt printer: Barcode on receipt | ⏳ |
| 489 | Cash drawer: Open on sale complete | ⏳ |
| 490 | Cash drawer: Manual open command | ⏳ |
| 491 | Cash drawer: Status indicator | ⏳ |
| 492 | Weighing scale: Serial port integration | ⏳ |
| 493 | Weighing scale: Auto-read weight | ⏳ |
| 494 | Weighing scale: Weight-based pricing | ⏳ |
| 495 | Customer display: Pole display support | ⏳ |
| 496 | Customer display: Show item added | ⏳ |
| 497 | Customer display: Show total | ⏳ |
| 498 | Customer display: Idle message | ⏳ |
| 499 | UPS/battery: Low battery warning | ⏳ |
| 500 | UPS/battery: Auto-save on power loss | ⏳ |
| 501 | Fingerprint reader: Biometric login | ⏳ |
| 502 | Fingerprint reader: Transaction auth | ⏳ |
| 503 | RFID reader: Tag scanning | ⏳ |
| 504 | RFID reader: Bulk inventory count | ⏳ |
| 505 | Camera: Product image capture | ⏳ |
| 506 | Camera: Customer ID scan | ⏳ |
| 507 | Signature pad: Digital signature capture | ⏳ |
| 508 | Signature pad: Refund authorization | ⏳ |
| 509 | NFC reader: Contactless card detect | ⏳ |
| 510 | NFC reader: Quick pay trigger | ⏳ |
| 511 | Multi-monitor: Dual screen support | ⏳ |
| 512 | Multi-monitor: Customer-facing display | ⏳ |
| 513 | Printer: A4 invoice printing | ⏳ |
| 514 | Printer: Gift receipt format | ⏳ |
| 515 | Printer: Delivery challan format | ⏳ |
| 516 | Hardware: Device health monitor | ⏳ |
| 517 | Hardware: Auto-reconnect on disconnect | ⏳ |
| 518 | Hardware: Configuration wizard | ⏳ |

### G2: AI & Intelligence (22 features)

| # | Feature | Status |
|---|---|---|
| 519 | AI: Demand forecasting per product | ⏳ |
| 520 | AI: Optimal reorder quantity suggestion | ⏳ |
| 521 | AI: Price optimization suggestion | ⏳ |
| 522 | AI: Customer churn prediction | ⏳ |
| 523 | AI: Product recommendation engine | ⏳ |
| 524 | AI: Sales anomaly detection | ⏳ |
| 525 | AI: Fraud detection (unusual discounts) | ⏳ |
| 526 | AI: Smart search (fuzzy matching) | ⏳ |
| 527 | AI: Auto-categorize products from name | ⏳ |
| 528 | AI: Seasonal trend detection | ⏳ |
| 529 | AI: Bundle/combo suggestions | ⏳ |
| 530 | AI: Customer segmentation | ⏳ |
| 531 | AI: Inventory health score | ⏳ |
| 532 | AI: Dynamic discount suggestions | ⏳ |
| 533 | AI: Natural language report queries | ⏳ |
| 534 | AI: Image-based product search | ⏳ |
| 535 | AI: Voice command support | ⏳ |
| 536 | AI: Chatbot assistant | ⏳ |
| 537 | AI: Auto-fill product details from barcode | ⏳ |
| 538 | AI: Predict peak sales hours | ⏳ |
| 539 | AI: Smart alert prioritization | ⏳ |
| 540 | AI: Auto-generate product descriptions | ⏳ |

### G3: Commercial & Licensing (27 features)

| # | Feature | Status |
|---|---|---|
| 541 | License: Key validation | ⏳ |
| 542 | License: Trial period (30 days) | ⏳ |
| 543 | License: Feature tier (Basic/Pro/Enterprise) | ⏳ |
| 544 | License: Online activation | ⏳ |
| 545 | License: Offline activation | ⏳ |
| 546 | License: Expiry warning | ⏳ |
| 547 | License: Grace period | ⏳ |
| 548 | License: User count limit | ⏳ |
| 549 | License: Store count limit | ⏳ |
| 550 | Subscription: Monthly billing | ⏳ |
| 551 | Subscription: Annual billing | ⏳ |
| 552 | Subscription: Usage analytics | ⏳ |
| 553 | Update: Auto-check for updates | ⏳ |
| 554 | Update: In-app update notification | ⏳ |
| 555 | Update: Delta update download | ⏳ |
| 556 | Update: Changelog display | ⏳ |
| 557 | Telemetry: Anonymous usage stats | ⏳ |
| 558 | Telemetry: Error reporting | ⏳ |
| 559 | Telemetry: Feature usage tracking | ⏳ |
| 560 | Branding: White-label support | ⏳ |
| 561 | Branding: Custom splash screen | ⏳ |
| 562 | Branding: Custom color scheme | ⏳ |
| 563 | Support: In-app ticket submission | ⏳ |
| 564 | Support: Remote assist connection | ⏳ |
| 565 | Support: Knowledge base link | ⏳ |
| 566 | Support: Video tutorial links | ⏳ |
| 567 | Support: Feedback form | ⏳ |

### G4: Localization (23 features)

| # | Feature | Status |
|---|---|---|
| 568 | L10n: Hindi (हिन्दी) UI translation | ⏳ |
| 569 | L10n: Tamil (தமிழ்) UI translation | ⏳ |
| 570 | L10n: Telugu (తెలుగు) UI translation | ⏳ |
| 571 | L10n: Kannada (ಕನ್ನಡ) UI translation | ⏳ |
| 572 | L10n: Malayalam (മലയാളം) UI translation | ⏳ |
| 573 | L10n: Marathi (मराठी) UI translation | ⏳ |
| 574 | L10n: Bengali (বাংলা) UI translation | ⏳ |
| 575 | L10n: Gujarati (ગુજરાતી) UI translation | ⏳ |
| 576 | L10n: Punjabi (ਪੰਜਾਬੀ) UI translation | ⏳ |
| 577 | L10n: Language switcher in settings | ⏳ |
| 578 | L10n: RTL layout support (Urdu) | ⏳ |
| 579 | L10n: Bilingual invoice (English + regional) | ⏳ |
| 580 | L10n: Number formatting per locale | ⏳ |
| 581 | L10n: Date formatting per locale | ⏳ |
| 582 | L10n: Currency symbol per locale | ⏳ |
| 583 | L10n: Resource file (.resx) architecture | ⏳ |
| 584 | L10n: Fallback to English | ⏳ |
| 585 | L10n: Regional tax labels (CGST/SGST/IGST) | ⏳ |
| 586 | L10n: Regional receipt format | ⏳ |
| 587 | L10n: Unicode font support | ⏳ |
| 588 | L10n: Transliteration input | ⏳ |
| 589 | L10n: SMS templates per language | ⏳ |
| 590 | L10n: Error messages per language | ⏳ |

### G5: Multi-Store (49 features)

| # | Feature | Status |
|---|---|---|
| 591 | Store model: Id, Name, Code, Address | ⏳ |
| 592 | Store: Phone, Email fields | ⏳ |
| 593 | Store: GSTIN per store | ⏳ |
| 594 | Store: IsActive flag | ⏳ |
| 595 | Store: Central database sync | ⏳ |
| 596 | Multi-store: Store selector on login | ⏳ |
| 597 | Multi-store: Store-scoped products | ⏳ |
| 598 | Multi-store: Store-scoped sales | ⏳ |
| 599 | Multi-store: Store-scoped inventory | ⏳ |
| 600 | Multi-store: Store-scoped users | ⏳ |
| 601 | Multi-store: Cross-store product catalog | ⏳ |
| 602 | Multi-store: Stock transfer request | ⏳ |
| 603 | Multi-store: Stock transfer approval | ⏳ |
| 604 | Multi-store: Stock transfer receipt | ⏳ |
| 605 | Multi-store: Transfer tracking | ⏳ |
| 606 | Multi-store: Inter-store pricing | ⏳ |
| 607 | Multi-store: Consolidated reports | ⏳ |
| 608 | Multi-store: Store comparison report | ⏳ |
| 609 | Multi-store: Central product management | ⏳ |
| 610 | Multi-store: Central pricing rules | ⏳ |
| 611 | Multi-store: Central promotion management | ⏳ |
| 612 | Multi-store: Store performance dashboard | ⏳ |
| 613 | Multi-store: Franchise model support | ⏳ |
| 614 | Multi-store: Warehouse/distribution center | ⏳ |
| 615 | Multi-store: Auto-replenishment from warehouse | ⏳ |
| 616 | Multi-store: Store-level profit center | ⏳ |
| 617 | Sync: Conflict resolution strategy | ⏳ |
| 618 | Sync: Offline queue with retry | ⏳ |
| 619 | Sync: Incremental sync | ⏳ |
| 620 | Sync: Full sync on demand | ⏳ |
| 621 | Sync: Sync status indicator | ⏳ |
| 622 | Sync: Last sync timestamp | ⏳ |
| 623 | Sync: Sync log viewer | ⏳ |
| 624 | Multi-store: Store grouping (region/zone) | ⏳ |
| 625 | Multi-store: Regional manager view | ⏳ |
| 626 | Multi-store: Head office admin view | ⏳ |
| 627 | Multi-store: Store opening/closing wizard | ⏳ |
| 628 | Multi-store: Store audit checklist | ⏳ |
| 629 | Multi-store: Inventory reconciliation | ⏳ |
| 630 | Multi-store: Price override per store | ⏳ |
| 631 | Multi-store: Customer shared across stores | ⏳ |
| 632 | Multi-store: Loyalty shared across stores | ⏳ |
| 633 | Multi-store: Return at any store | ⏳ |
| 634 | Multi-store: Gift card redeemable anywhere | ⏳ |
| 635 | Multi-store: Consolidated GST filing | ⏳ |
| 636 | Multi-store: Store-level notifications | ⏳ |
| 637 | Multi-store: Real-time stock visibility | ⏳ |
| 638 | Multi-store: Store ranking dashboard | ⏳ |
| 639 | Multi-store: Bulk product push to stores | ⏳ |

### G6: E-commerce Integration (46 features)

| # | Feature | Status |
|---|---|---|
| 640 | Ecom: WooCommerce connector | ⏳ |
| 641 | Ecom: Shopify connector | ⏳ |
| 642 | Ecom: Amazon Seller connector | ⏳ |
| 643 | Ecom: Flipkart Seller connector | ⏳ |
| 644 | Ecom: Myntra Seller connector | ⏳ |
| 645 | Ecom: Product catalog sync (push) | ⏳ |
| 646 | Ecom: Price sync (push) | ⏳ |
| 647 | Ecom: Stock level sync (push) | ⏳ |
| 648 | Ecom: Order import (pull) | ⏳ |
| 649 | Ecom: Order status update | ⏳ |
| 650 | Ecom: Shipping label generation | ⏳ |
| 651 | Ecom: Return/refund sync | ⏳ |
| 652 | Ecom: Channel-wise sales report | ⏳ |
| 653 | Ecom: Channel-wise inventory allocation | ⏳ |
| 654 | Ecom: Marketplace fee tracking | ⏳ |
| 655 | Ecom: SKU mapping (POS ↔ marketplace) | ⏳ |
| 656 | Ecom: Image sync | ⏳ |
| 657 | Ecom: Bulk listing update | ⏳ |
| 658 | Ecom: Order fulfillment workflow | ⏳ |
| 659 | Ecom: Pick-pack-ship process | ⏳ |
| 660 | Ecom: AWB (airway bill) tracking | ⏳ |
| 661 | Ecom: Courier partner integration | ⏳ |
| 662 | Ecom: Delhivery connector | ⏳ |
| 663 | Ecom: Shiprocket connector | ⏳ |
| 664 | Ecom: Delivery status tracking | ⏳ |
| 665 | Ecom: COD reconciliation | ⏳ |
| 666 | Ecom: Omnichannel inventory view | ⏳ |
| 667 | Ecom: Online-offline price parity | ⏳ |
| 668 | Ecom: Channel priority for stock allocation | ⏳ |
| 669 | Ecom: Order notification | ⏳ |
| 670 | Ecom: Abandoned cart data (Shopify/Woo) | ⏳ |
| 671 | Ecom: Customer import from channels | ⏳ |
| 672 | Ecom: Review/rating import | ⏳ |
| 673 | Ecom: SEO metadata for products | ⏳ |
| 674 | Ecom: Discount coupon sync | ⏳ |
| 675 | Ecom: Flash sale scheduling | ⏳ |
| 676 | Ecom: Variant management (size/color) | ⏳ |
| 677 | Ecom: Bundle/kit products | ⏳ |
| 678 | Ecom: Pre-order support | ⏳ |
| 679 | Ecom: Backorder support | ⏳ |
| 680 | Ecom: Wish list data import | ⏳ |
| 681 | Ecom: Gift wrapping option | ⏳ |
| 682 | Ecom: Digital invoice via email | ⏳ |
| 683 | Ecom: Webhook listener | ⏳ |
| 684 | Ecom: Rate limiting & throttling | ⏳ |
| 685 | Ecom: Sync dashboard | ⏳ |

### G7: API & Integration (21 features)

| # | Feature | Status |
|---|---|---|
| 686 | API: REST endpoint — GET products | ⏳ |
| 687 | API: REST endpoint — GET sales | ⏳ |
| 688 | API: REST endpoint — POST sale | ⏳ |
| 689 | API: REST endpoint — GET inventory | ⏳ |
| 690 | API: REST endpoint — GET customers | ⏳ |
| 691 | API: REST endpoint — GET reports | ⏳ |
| 692 | API: Authentication (API key) | ⏳ |
| 693 | API: Rate limiting | ⏳ |
| 694 | API: Swagger/OpenAPI documentation | ⏳ |
| 695 | API: Webhook outbound (sale complete) | ⏳ |
| 696 | API: Webhook outbound (stock change) | ⏳ |
| 697 | API: Tally ERP integration | ⏳ |
| 698 | API: Busy Accounting integration | ⏳ |
| 699 | API: WhatsApp Business API | ⏳ |
| 700 | API: SMS gateway (MSG91/Twilio) | ⏳ |
| 701 | API: Email gateway (SendGrid/SMTP) | ⏳ |
| 702 | API: Google Sheets export | ⏳ |
| 703 | API: Zapier/Make connector | ⏳ |
| 704 | API: Power BI connector | ⏳ |
| 705 | API: Excel add-in support | ⏳ |
| 706 | API: Health check endpoint | ⏳ |

### G8: Mobile Companion (11 features)

| # | Feature | Status |
|---|---|---|
| 707 | Mobile: Owner dashboard app (read-only) | ⏳ |
| 708 | Mobile: Real-time sales notification | ⏳ |
| 709 | Mobile: Stock alerts push notification | ⏳ |
| 710 | Mobile: Daily summary push | ⏳ |
| 711 | Mobile: Quick stock check | ⏳ |
| 712 | Mobile: Quick price lookup | ⏳ |
| 713 | Mobile: Approve refund request | ⏳ |
| 714 | Mobile: Approve stock transfer | ⏳ |
| 715 | Mobile: View today's sales chart | ⏳ |
| 716 | Mobile: Remote store monitoring | ⏳ |
| 717 | Mobile: Camera barcode scan | ⏳ |

### G9: Multi-Country (9 features)

| # | Feature | Status |
|---|---|---|
| 718 | Country: Tax regime configuration | ⏳ |
| 719 | Country: VAT support (non-GST) | ⏳ |
| 720 | Country: Sales tax support (US) | ⏳ |
| 721 | Country: Multi-currency support | ⏳ |
| 722 | Country: Exchange rate management | ⏳ |
| 723 | Country: Address format per country | ⏳ |
| 724 | Country: Phone format validation | ⏳ |
| 725 | Country: Regional compliance rules | ⏳ |
| 726 | Country: Country selector on setup | ⏳ |

### G10: Niche Vertical — Clothing Retail (61 features)

| # | Feature | Status |
|---|---|---|
| 727 | Product: Size field | ⏳ |
| 728 | Product: Size chart (S/M/L/XL/XXL) | ⏳ |
| 729 | Product: Size-wise stock tracking | ⏳ |
| 730 | Product: Color-size matrix (SKU grid) | ⏳ |
| 731 | Product: Fabric/material field | ⏳ |
| 732 | Product: Season field (Summer/Winter/Monsoon/AllSeason) | ⏳ |
| 733 | Product: Style code field | ⏳ |
| 734 | Product: Design/pattern field | ⏳ |
| 735 | Product: Gender (Men/Women/Kids/Unisex) | ⏳ |
| 736 | Product: Occasion (Casual/Formal/Party/Wedding) | ⏳ |
| 737 | Product: Fit type (Slim/Regular/Loose) | ⏳ |
| 738 | Product: Sleeve type (Full/Half/Sleeveless) | ⏳ |
| 739 | Product: Neck type (Round/V/Collar) | ⏳ |
| 740 | Product: Wash care instructions | ⏳ |
| 741 | Product: Manufacturer/importer field | ⏳ |
| 742 | Product: Country of origin | ⏳ |
| 743 | Product: Product image gallery | ⏳ |
| 744 | Product: Variant grouping (parent-child) | ⏳ |
| 745 | Product: Combo/set products | ⏳ |
| 746 | Product: Alteration tracking | ⏳ |
| 747 | Product: Alteration charges | ⏳ |
| 748 | Sales: Size selection in cart | ⏳ |
| 749 | Sales: Color-size picker modal | ⏳ |
| 750 | Sales: Trial room tracking | ⏳ |
| 751 | Sales: Alteration order creation | ⏳ |
| 752 | Sales: Alteration delivery date | ⏳ |
| 753 | Sales: Gift wrapping option | ⏳ |
| 754 | Sales: Gift message field | ⏳ |
| 755 | Sales: Exchange mode (return + new sale) | ⏳ |
| 756 | Sales: Advance payment/layaway | ⏳ |
| 757 | Sales: Credit sale (pay later) | ⏳ |
| 758 | Inventory: Size-wise reorder | ⏳ |
| 759 | Inventory: Color-wise reorder | ⏳ |
| 760 | Inventory: Season end clearance tagging | ⏳ |
| 761 | Inventory: Dead stock by season | ⏳ |
| 762 | Reports: Size-wise sales analysis | ⏳ |
| 763 | Reports: Color-wise sales analysis | ⏳ |
| 764 | Reports: Season performance report | ⏳ |
| 765 | Reports: Gender-wise sales split | ⏳ |
| 766 | Reports: Style code performance | ⏳ |
| 767 | Reports: Alteration revenue report | ⏳ |
| 768 | Reports: Exchange/return rate by category | ⏳ |
| 769 | Barcode: Style-Color-Size encoded | ⏳ |
| 770 | Label: Price tag with size/color | ⏳ |
| 771 | Label: Wash care symbol printing | ⏳ |
| 772 | Display: Mannequin/display stock tracking | ⏳ |
| 773 | Display: Display stock lock (non-sellable) | ⏳ |
| 774 | Loyalty: Birthday discount auto-apply | ⏳ |
| 775 | Loyalty: Anniversary offer | ⏳ |
| 776 | Loyalty: Festival-linked promotions | ⏳ |
| 777 | Seasonal: Diwali sale template | ⏳ |
| 778 | Seasonal: Eid sale template | ⏳ |
| 779 | Seasonal: Christmas sale template | ⏳ |
| 780 | Seasonal: Republic Day sale template | ⏳ |
| 781 | Seasonal: End of season sale wizard | ⏳ |
| 782 | Consignment: Vendor consignment tracking | ⏳ |
| 783 | Consignment: Commission calculation | ⏳ |
| 784 | Consignment: Unsold return to vendor | ⏳ |
| 785 | Catalogue: Digital lookbook generation | ⏳ |
| 786 | Catalogue: WhatsApp catalogue share | ⏳ |
| 787 | Catalogue: PDF catalogue export | ⏳ |

### G11: Touch & Kiosk (11 features)

| # | Feature | Status |
|---|---|---|
| 788 | Touch: Large button billing mode | ⏳ |
| 789 | Touch: On-screen numpad | ⏳ |
| 790 | Touch: Swipe to delete cart item | ⏳ |
| 791 | Touch: Pinch-zoom on reports | ⏳ |
| 792 | Touch: Touch-friendly ComboBox | ⏳ |
| 793 | Touch: Full-screen kiosk mode | ⏳ |
| 794 | Touch: Auto-hide title bar | ⏳ |
| 795 | Kiosk: Customer self-checkout | ⏳ |
| 796 | Kiosk: Product browsing | ⏳ |
| 797 | Kiosk: Price check station | ⏳ |
| 798 | Kiosk: Locked-down Windows shell | ⏳ |

### G12: CRM — Customer Relationship (25 features)

| # | Feature | Status |
|---|---|---|
| 799 | CRM: Customer 360° view | ⏳ |
| 800 | CRM: Purchase history timeline | ⏳ |
| 801 | CRM: Communication log | ⏳ |
| 802 | CRM: Customer tags/labels | ⏳ |
| 803 | CRM: Customer groups/segments | ⏳ |
| 804 | CRM: RFM analysis (Recency/Frequency/Monetary) | ⏳ |
| 805 | CRM: Targeted promotion by segment | ⏳ |
| 806 | CRM: SMS campaign builder | ⏳ |
| 807 | CRM: WhatsApp message template | ⏳ |
| 808 | CRM: Email campaign builder | ⏳ |
| 809 | CRM: Campaign performance tracking | ⏳ |
| 810 | CRM: Feedback collection | ⏳ |
| 811 | CRM: NPS (Net Promoter Score) tracking | ⏳ |
| 812 | CRM: Complaint management | ⏳ |
| 813 | CRM: Gift card issuance | ⏳ |
| 814 | CRM: Gift card redemption | ⏳ |
| 815 | CRM: Gift card balance check | ⏳ |
| 816 | CRM: Store credit management | ⏳ |
| 817 | CRM: Referral program | ⏳ |
| 818 | CRM: Customer import from Excel | ⏳ |
| 819 | CRM: Duplicate customer merge | ⏳ |
| 820 | CRM: Customer export | ⏳ |
| 821 | CRM: Wishlist tracking | ⏳ |
| 822 | CRM: Follow-up reminders | ⏳ |
| 823 | CRM: VIP customer flagging | ⏳ |

### G13: HR & Staff (8 features)

| # | Feature | Status |
|---|---|---|
| 824 | HR: Employee profile model | ⏳ |
| 825 | HR: Attendance clock-in/clock-out | ⏳ |
| 826 | HR: Shift management | ⏳ |
| 827 | HR: Sales target per employee | ⏳ |
| 828 | HR: Commission calculation | ⏳ |
| 829 | HR: Performance report per cashier | ⏳ |
| 830 | HR: Leave management | ⏳ |
| 831 | HR: Salary/commission payout report | ⏳ |

### G14: Compliance & Legal (17 features)

| # | Feature | Status |
|---|---|---|
| 832 | GST: GSTR-1 data preparation | ⏳ |
| 833 | GST: GSTR-3B data preparation | ⏳ |
| 834 | GST: HSN-wise summary | ⏳ |
| 835 | GST: B2B invoice format | ⏳ |
| 836 | GST: B2C invoice format | ⏳ |
| 837 | GST: E-invoice generation (IRN) | ⏳ |
| 838 | GST: E-way bill generation | ⏳ |
| 839 | GST: Credit/debit note | ⏳ |
| 840 | GST: Reverse charge mechanism | ⏳ |
| 841 | GST: Composition scheme support | ⏳ |
| 842 | Legal: Consumer protection compliance | ⏳ |
| 843 | Legal: MRP display enforcement | ⏳ |
| 844 | Legal: Weights & measures compliance | ⏳ |
| 845 | Legal: Data privacy (customer consent) | ⏳ |
| 846 | Legal: Digital signature on invoices | ⏳ |
| 847 | Legal: Audit trail immutability | ⏳ |
| 848 | Legal: FSSAI compliance (if applicable) | ⏳ |

### G15: UI Polish & Accessibility (18 features)

| # | Feature | Status |
|---|---|---|
| 849 | UI: Dark mode theme | ⏳ |
| 850 | UI: Light mode theme | ✅ |
| 851 | UI: High contrast mode | ⏳ |
| 852 | UI: Font size scaling (Small/Normal/Large) | ⏳ |
| 853 | UI: Keyboard-only navigation (all views) | ✅ |
| 854 | UI: Screen reader compatibility | ⏳ |
| 855 | UI: Tab order optimization | ⏳ |
| 856 | UI: Tooltip system | ✅ |
| 857 | UI: Loading skeleton placeholders | ⏳ |
| 858 | UI: Micro-animations (slide/fade) | ✅ |
| 859 | UI: Empty state illustrations | ⏳ |
| 860 | UI: Success/error toast notifications | ⏳ |
| 861 | UI: Confirmation dialogs (delete/exit) | ✅ |
| 862 | UI: Breadcrumb navigation | ⏳ |
| 863 | UI: Sidebar collapse/expand animation | ✅ |
| 864 | UI: Responsive layout (resize) | ✅ |
| 865 | UI: Print preview before print | ⏳ |
| 866 | UI: Drag-drop column reorder | ⏳ |

### G16: Database Administration (14 features)

| # | Feature | Status |
|---|---|---|
| 867 | DB: SQLite → SQL Server migration tool | ⏳ |
| 868 | DB: Data export (full dump) | ⏳ |
| 869 | DB: Data import (full restore) | ⏳ |
| 870 | DB: Table row count dashboard | ⏳ |
| 871 | DB: Database size display | ⏳ |
| 872 | DB: Index health check | ⏳ |
| 873 | DB: Vacuum/compact SQLite | ⏳ |
| 874 | DB: Scheduled backup to cloud | ⏳ |
| 875 | DB: Backup to USB drive | ⏳ |
| 876 | DB: Version migration runner | ✅ |
| 877 | DB: Seed data for demo mode | ⏳ |
| 878 | DB: Data anonymization tool | ⏳ |
| 879 | DB: Connection string configuration | ⏳ |
| 880 | DB: Multi-tenant schema support | ⏳ |

### G17: Documents & Templates (9 features)

| # | Feature | Status |
|---|---|---|
| 881 | Doc: Invoice template editor | ⏳ |
| 882 | Doc: Receipt template editor | ⏳ |
| 883 | Doc: Quotation/estimate template | ⏳ |
| 884 | Doc: Purchase order template | ⏳ |
| 885 | Doc: Delivery challan template | ⏳ |
| 886 | Doc: Credit note template | ⏳ |
| 887 | Doc: Barcode label template editor | ⏳ |
| 888 | Doc: Letterhead configuration | ⏳ |
| 889 | Doc: Terms & conditions text | ⏳ |

### G18: Workflows & Automation (8 features)

| # | Feature | Status |
|---|---|---|
| 890 | Workflow: Low stock auto-purchase order | ⏳ |
| 891 | Workflow: Sale complete → print + email | ⏳ |
| 892 | Workflow: Return → manager approval | ⏳ |
| 893 | Workflow: Day-end auto-report email | ⏳ |
| 894 | Workflow: Scheduled report generation | ⏳ |
| 895 | Workflow: Price change approval | ⏳ |
| 896 | Workflow: Bulk operation approval | ⏳ |
| 897 | Workflow: Custom trigger builder | ⏳ |

### G19: Preferences & Personalization (19 features)

| # | Feature | Status |
|---|---|---|
| 898 | Pref: Default landing page | ⏳ |
| 899 | Pref: Sidebar pinned items | ⏳ |
| 900 | Pref: DataGrid column visibility toggle | ⏳ |
| 901 | Pref: DataGrid column order persistence | ⏳ |
| 902 | Pref: Default page size per view | ⏳ |
| 903 | Pref: Default sort per view | ⏳ |
| 904 | Pref: Quick action bar customization | ⏳ |
| 905 | Pref: Notification preferences | ⏳ |
| 906 | Pref: Sound effects (sale complete beep) | ⏳ |
| 907 | Pref: Auto-focus behavior | ⏳ |
| 908 | Pref: Keyboard shortcut customization | ⏳ |
| 909 | Pref: Date range default (Today/Week/Month) | ⏳ |
| 910 | Pref: Dashboard widget arrangement | ⏳ |
| 911 | Pref: Receipt auto-print toggle | ⏳ |
| 912 | Pref: Cash drawer auto-open toggle | ⏳ |
| 913 | Pref: Low stock threshold per user | ⏳ |
| 914 | Pref: Number of recent items | ⏳ |
| 915 | Pref: Compact/comfortable view density | ⏳ |
| 916 | Pref: User avatar/profile picture | ⏳ |

### G20: Payment Integration (17 features)

| # | Feature | Status |
|---|---|---|
| 917 | Pay: UPI deep link (PhonePe/GPay/Paytm) | ⏳ |
| 918 | Pay: UPI QR code generation | ⏳ |
| 919 | Pay: UPI payment verification | ⏳ |
| 920 | Pay: Razorpay POS integration | ⏳ |
| 921 | Pay: PayTM POS integration | ⏳ |
| 922 | Pay: Pine Labs EDC integration | ⏳ |
| 923 | Pay: Card swipe machine integration | ⏳ |
| 924 | Pay: Split payment (Cash + Card) | ⏳ |
| 925 | Pay: Split payment (Cash + UPI) | ⏳ |
| 926 | Pay: Partial payment / advance | ⏳ |
| 927 | Pay: Credit sale (due balance) | ⏳ |
| 928 | Pay: EMI option | ⏳ |
| 929 | Pay: Wallet integration (Paytm Wallet) | ⏳ |
| 930 | Pay: Payment reconciliation | ⏳ |
| 931 | Pay: Settlement report | ⏳ |
| 932 | Pay: Refund to original payment method | ⏳ |
| 933 | Pay: Payment failure retry | ⏳ |

### G21: Budgeting & Forecasting (6 features)

| # | Feature | Status |
|---|---|---|
| 934 | Budget: Monthly revenue target | ⏳ |
| 935 | Budget: Monthly expense budget | ⏳ |
| 936 | Budget: Budget vs actual dashboard | ⏳ |
| 937 | Budget: Variance analysis | ⏳ |
| 938 | Forecast: Next month sales prediction | ⏳ |
| 939 | Forecast: Cash flow projection | ⏳ |

### G22: Advanced Reporting (9 features)

| # | Feature | Status |
|---|---|---|
| 940 | Report: Custom report builder | ⏳ |
| 941 | Report: Saved report templates | ⏳ |
| 942 | Report: Scheduled email delivery | ⏳ |
| 943 | Report: Chart/graph visualization | ⏳ |
| 944 | Report: Drill-down from summary to detail | ⏳ |
| 945 | Report: Cross-tab/pivot table | ⏳ |
| 946 | Report: Comparison period overlay | ⏳ |
| 947 | Report: Dashboard widget from any report | ⏳ |
| 948 | Report: Real-time auto-refresh | ⏳ |

---

## Summary

| Category | Total | ✅ Done | ⏭ Skip | ⏳ Pending |
|---|---|---|---|---|
| **Phase 1** — Foundation & Product | 72 | 70 | 2 | 0 |
| **Phase 2** — Sales & Billing | 87 | 87 | 0 | 0 |
| **Phase 3** — Inventory & Stock | 85 | 28 | 0 | 57 |
| **Phase 4** — Customer & Relationships | 83 | 0 | 0 | 83 |
| **Phase 5** — Reports & Analytics | 82 | 22 | 0 | 60 |
| **Phase 6** — Settings & Admin | 61 | 26 | 0 | 35 |
| **Core Total** | **470** | **233** | **2** | **235** |
| **G1–G22 Expansion** | **478** | **8** | **0** | **470** |
| **Grand Total** | **948** | **241** | **2** | **705** |

> **Progress: 241/948 (25.4%)**
>
> Last updated after Feature #130 session.
