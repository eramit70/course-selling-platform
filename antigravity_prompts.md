# Antigravity Build Guide — Trading Live Course Platform (.NET + Azure)

Run these prompts **one at a time**, in order, in a new Antigravity project. Wait for each stage to finish, review the diff/artifact it produces, then move to the next. Don't paste them all at once — staged prompts give the agent a much smaller, verifiable surface per step, which matters most for the payment and auth stages.

A one-shot combined prompt is included at the very end if you want to try that route instead, but the staged approach is the safer default.

---

## Step 0 — Project scaffold

```
Create a new ASP.NET Core MVC (.NET 8) web application named TradingCourse.Web.
Use the Areas feature: a default Area for the public site, and an "Admin" Area for the admin panel.
Set up EF Core with SQL Server, using a connection string named "DefaultConnection" in appsettings.json (leave it as a placeholder, I will point it to Azure SQL later).
Add a Data folder with an AppDbContext.
Add a Services folder for business logic (empty for now) and a Models folder for entities (empty for now).
Set up basic Serilog logging to console and a rolling file.
Do not add any business logic yet — this step is only project scaffolding, folder structure, and NuGet packages (Microsoft.EntityFrameworkCore.SqlServer, Serilog.AspNetCore).
```

---

## Step 1 — Domain models & database

```
In TradingCourse.Web, add the following EF Core entities under Models/:
- Course: Id, Title, Slug, ShortDescription, FullDescription, ThumbnailUrl, Price, SalePrice (nullable), IsLive (bool), Instructor, Schedule, LiveClassLink, EmailTemplateSubject, EmailTemplateBody, CreatedAt, UpdatedAt.
- Coupon: Id, Code (unique), DiscountType (enum: Percentage/Fixed), DiscountValue, ExpiryDate (nullable), MaxUsage (nullable), UsedCount, MinPurchaseAmount (nullable), CourseId (nullable, null = applies to all courses).
- Order: Id, CourseId, CustomerName, CustomerEmail, CustomerMobile, CouponCode (nullable), AmountPaid, RazorpayOrderId, RazorpayPaymentId, PaymentStatus (enum: Pending/Success/Failed), CreatedAt.
- AdminUser: use ASP.NET Core Identity's IdentityUser, extended if needed, with a single "Admin" role.

Wire these into AppDbContext with EF Core Code-First migrations. Add a unique index on Course.Slug, Coupon.Code, and a non-unique index on Order.CustomerEmail (needed later for duplicate-purchase checks).
Generate the initial migration but do not apply it yet — I'll point the connection string to Azure SQL first.
```

---

## Step 2 — Public site: course catalog & details

```
Build the public-facing pages in the default Area:
1. Home/Landing page: shows a hero section for the currently advertised course (pulled from the Course marked as featured, or the most recent live course), with a "View all courses" button.
2. Course catalog page: grid of all courses where IsLive = true, each card showing thumbnail, title, price (show SalePrice struck-through against Price if set), and a "View details" link.
3. Course details page (by slug): full description, instructor, schedule, price/discount, and a "Buy now" button that goes to a checkout page.
4. Checkout page: form for CustomerName, CustomerEmail, CustomerMobile, a coupon code field with an "Apply" button that recalculates the price via an AJAX call (no page reload), and an order summary.

Use a CourseService in the Services folder for all data access from these pages — controllers should stay thin.
Style with Bootstrap (already included in the default template). Keep it clean and mobile-first since most traffic comes from Instagram.
```

---

## Step 3 — Admin area & secured access

```
Set up ASP.NET Core Identity for the Admin area only — there is no public registration page anywhere in the app.
Restrict every controller/page under Areas/Admin with [Authorize(Roles = "Admin")].
Add a database seeder (runs on startup in Development, and as a one-time migration step in Production) that creates one Admin user from configuration values (AdminEmail / AdminPassword in appsettings — I will override these as environment variables in Azure).

Build these Admin pages:
1. Course management: list, create, edit, delete courses, including the email template subject/body fields with placeholders like {CustomerName}, {LiveClassLink}, {Schedule}.
2. Coupon management: list, create, edit, deactivate coupons, with usage count shown.
3. Orders/leads: list of all orders with payment status, customer details, filter by course and date, and CSV export.

Add a CourseService and CouponService method set that both the public site and admin controllers reuse (don't duplicate logic between them).
```

---

## Step 4 — Razorpay payment integration

```
Add Razorpay payment integration to the checkout flow:
1. On "Buy now" submit, create a Razorpay Order server-side (via the Razorpay .NET SDK) using the final price (after coupon discount), and return the order id to the client to open Razorpay Checkout.
2. On payment completion, verify the payment signature server-side before marking the Order as Success — never trust a client-side "success" callback alone.
3. Add a webhook endpoint (e.g. /webhooks/razorpay) that Razorpay can call directly to confirm payment status as a second, independent confirmation path, and update the Order accordingly if the client-side confirmation didn't arrive.
4. Keep the Razorpay key id and key secret out of appsettings.json — read them from configuration keys RazorpaySettings:KeyId and RazorpaySettings:KeySecret, which I will set as environment variables / Azure App Service configuration, not commit to source control.
5. Handle payment failure gracefully — show a clear retry option on the checkout page, and log the failure reason.
```

---

## Step 5 — Email automation

```
Add a background email queue (use IHostedService with an in-memory queue, or Hangfire if you prefer a retry-capable dashboard — your choice, explain the trade-off briefly before implementing) so emails are sent after payment success without blocking the checkout response.

Add an EmailService that:
1. On Order.PaymentStatus becoming Success, sends an email to CustomerEmail using the course's EmailTemplateSubject/EmailTemplateBody, replacing placeholders like {CustomerName}, {LiveClassLink}, {Schedule}, {InstructorName}.
2. Sends a separate notification email to a configured admin address (or a per-course "OwnerEmail" field if you want to extend the Course model) with the buyer's name, email, mobile, course purchased, and amount paid.
3. Use MailKit for SMTP, reading SMTP host/port/username/password from configuration (again, not hardcoded — environment variables in Azure).
4. Log every email send attempt and failure so I can debug delivery issues later.
```

---

## Step 6 — Coupons, discounts & validation

```
Add these validations across the public checkout flow (both client-side for UX and server-side as the source of truth):
1. Email format validation, and a duplicate-purchase check: block (or flag, make this configurable) a CustomerEmail from buying the same Course twice, based on existing successful Orders.
2. Mobile number format validation (10-digit Indian mobile numbers, with basic country-code handling).
3. Coupon validation: check expiry date, usage limit, minimum purchase amount, and whether it's restricted to a specific course, recalculating the final price server-side (never trust a client-sent discounted amount).
4. All these validations must run server-side even if client-side validation also exists, since the checkout API endpoint could be called directly.

Add clear, specific error messages returned to the checkout page for each validation failure case.
```

---

## Step 7 — Azure hosting setup

```
Prepare this project for deployment to Azure App Service with Azure SQL Database:
1. Update the DefaultConnection connection string format to match Azure SQL (Server=tcp:<server>.database.windows.net,1433; ... Encrypt=True;).
2. Make sure all secrets (Razorpay keys, SMTP credentials, Admin seed password, SQL connection string) are read from configuration/environment variables only — never hardcoded — so they can be set via Azure App Service's "Configuration > Application settings" or Azure Key Vault references.
3. Add a health check endpoint (/health) so Azure can monitor app status.
4. Add appsettings.Production.json with placeholders (no real secrets) and confirm the app reads environment-specific config correctly based on ASPNETCORE_ENVIRONMENT.
5. Add a GitHub Actions workflow (.github/workflows/azure-deploy.yml) that: builds the project, runs `dotnet ef database update` against the target Azure SQL database using a connection string from a GitHub secret, publishes the app, and deploys to Azure App Service using the publish profile (I will add AZURE_WEBAPP_PUBLISH_PROFILE as a GitHub secret).
6. Document in a short DEPLOYMENT.md what environment variables/App Settings need to be set in the Azure Portal before first deploy (Razorpay keys, SMTP settings, Admin seed credentials, connection string).
```

---

## Step 8 — Final review pass

```
Do a final review pass across the whole project:
1. Confirm no secrets, API keys, or passwords are hardcoded anywhere in source control.
2. Confirm every Admin route is actually protected by [Authorize(Roles = "Admin")] — list them out so I can double check.
3. Confirm the Razorpay payment signature verification happens server-side and cannot be bypassed by a crafted client request.
4. Confirm coupon price recalculation happens server-side at the time of order creation, not just client-side display.
5. Add basic error pages (500, 404) and make sure unhandled exceptions don't leak stack traces in Production.
6. Summarize any assumptions you made that I should double-check before going live.
```

---

## Alternative: one-shot combined prompt

If you'd rather try a single pass instead of staged steps (higher risk of the agent missing details, especially around payment security — review the diff carefully if you go this route):

```
Build a complete ASP.NET Core MVC (.NET 8) application named TradingCourse.Web for selling live trading courses, to be hosted on Azure App Service with Azure SQL Database. Requirements:

- Single deployable app using Areas: default Area = public site, "Admin" Area = secured admin panel (no public registration, one seeded admin account via ASP.NET Core Identity, protected with [Authorize(Roles="Admin")]).
- Public site: landing page featuring the currently advertised course, a course catalog grid of all live courses, course details pages, and a checkout flow (customer details + coupon code + Razorpay Checkout).
- Razorpay integration: server-side order creation, server-side payment signature verification, and a webhook endpoint as a second confirmation path. Keys read from configuration/environment variables only.
- On payment success: queue a background email to the customer using a per-course configurable template (with placeholders like {CustomerName}, {LiveClassLink}, {Schedule}), and a separate notification email to the admin, both via MailKit/SMTP with credentials from configuration.
- Coupon/discount module: percentage or fixed discount, expiry date, usage limits, minimum purchase amount, per-course or global, validated and recalculated server-side.
- Validation: email format + duplicate-purchase check per course, mobile number format validation, all enforced server-side.
- Admin panel: course CRUD (including email template fields), coupon CRUD, orders/leads list with filters and CSV export.
- EF Core Code-First with SQL Server/Azure SQL, Serilog logging, a /health endpoint, and a GitHub Actions workflow that builds, runs EF Core migrations, and deploys to Azure App Service via publish profile.
- No hardcoded secrets anywhere — everything sensitive comes from configuration/environment variables/Azure App Settings.

Work through this as a clear implementation plan first, then execute step by step, and flag anywhere you had to make an assumption.
```
