# Deployment Guidelines: APSRA Trading Course Platform

This document describes how to deploy the .NET 9 hybrid trading platform onto Azure App Services and Azure SQL Server.

---

## 1. Prerequisites
- An active **Microsoft Azure** account.
- A **Razorpay** dashboard account (Live API Keys).
- An **SMTP Service Provider** credentials (Host, Port, Username, Password) for transactional emails.

---

## 2. Infrastructure Setup (Azure Portal)

### A. Azure SQL Database
1. Create a new **Azure SQL Database** Server.
2. Configure the server firewall:
   - Check **"Allow Azure services and resources to access this server"** to let the App Service connect.
   - Add your local client IP to the firewall rules for local developer management.
3. Create a blank database named `TradingCourseDb`.
4. Copy the connection string:
   ```connectionstring
   Server=tcp:yourserver.database.windows.net,1433;Initial Catalog=TradingCourseDb;Persist Security Info=False;User ID=yourusername;Password=yourpassword;MultipleActiveResultSets=True;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;
   ```

### B. Azure App Service
1. Create a new **Web App**:
   - **Publish**: Code
   - **Runtime Stack**: `.NET 9 (LTS)`
   - **Operating System**: Windows (or Linux)
2. Choose your pricing tier (Basic B1/B2 recommended for staging, Standard S1 for production).

---

## 3. Configuration & Environment Variables

Configure the following application keys inside the Azure Web App under **Settings > Configuration > Application Settings**:

| Setting Key | Type | Description |
| :--- | :--- | :--- |
| `ConnectionStrings:DefaultConnection` | Connection String | Your Azure SQL connection string (SQLServer type) |
| `Razorpay:KeyId` | App Setting | Your Razorpay API Key ID (`rzp_live_...`) |
| `Razorpay:KeySecret` | App Setting | Your Razorpay API Secret |
| `Razorpay:WebhookSecret` | App Setting | Cryptographic signature secret for Razorpay webhooks |
| `Smtp:Host` | App Setting | SMTP Host Address (e.g. `smtp.gmail.com`) |
| `Smtp:Port` | App Setting | SMTP Port (e.g. `587` or `465`) |
| `Smtp:Username` | App Setting | SMTP Account Username (e.g. `sender@tradingacademy.com`) |
| `Smtp:Password` | App Setting | SMTP Account Password |
| `Smtp:FromName` | App Setting | Display name of the sender |
| `AdminSettings:DefaultEmail` | App Setting | Initial system Admin email created on startup |
| `AdminSettings:DefaultPassword`| App Setting | Initial system Admin password (must be >= 6 chars) |

---

## 4. Continuous Deployment via GitHub Actions

1. Retrieve the **Publish Profile** from your Azure Web App (click "Get Publish Profile" on the overview blade).
2. Go to your **GitHub Repository** settings:
   - Navigate to **Settings > Secrets and variables > Actions**.
   - Create a new repository secret: `AZURE_WEBAPP_PUBLISH_PROFILE`.
   - Paste the XML contents of your Publish Profile.
3. Commit and push the `.github/workflows/azure-deploy.yml` file to the `main` branch.
4. Push events will now automatically compile, test, package, and deploy your site to Azure.

---

## 5. First-Time Initialization
- On first start, the `DbInitializer.SeedAsync()` routine automatically executes pending database migrations against Azure SQL and seeds the default Admin credential record.
- **Hangfire** is fully active at `/admin/hangfire` using the Azure SQL database as a backing job store.
- **Health Checks** endpoint is exposed at `/health`. Add this URL to Azure App Service **Monitoring > Health check** blade to configure automated self-healing.
