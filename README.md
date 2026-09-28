# T&JPork 🥩

**T&JPork** is a modern online butchery store for premium, pasture-raised artisanal pork products. It allows customers to browse cuts of pork, add items to their cart, select a delivery date, and place orders. It also includes an admin dashboard for managing inventory, tracking orders, and updating store information.

---

## 🌟 Key Features

### For Customers
- **Product Catalog**: Browse pork cuts by category (Roasts, Chops, Bacon, Sausages, Ribs, etc.) with real-time stock levels and discounts.
- **Shopping Cart**: View items, calculate subtotals, apply discount codes, and see how much more you need for free delivery.
- **Easy Checkout**: Choose delivery dates, select morning/afternoon time slots, add special instructions, and pick payment options.
- **Customer Profiles**: View past orders, track delivery status, manage saved shipping addresses, and receive order notifications.
- **Reviews**: Leave ratings and reviews on products you purchased.

### For Admins
- **Inventory & Products**: Add new products, update prices, adjust stock, and upload photos.
- **Order Management**: View incoming orders, change delivery status (Processing, Packed, Shipped, Delivered), and update payment status.
- **Store Settings & CMS**: Update "About Us" stories, founder pictures, business hours, and contact details directly from the dashboard.

---

## 🛠️ Built With

- **Backend & Web**: ASP.NET Core MVC (.NET 10)
- **Database**: 
  - **Supabase PostgreSQL** in production (data stays safe forever)
  - **SQLite** fallback for easy local offline development
- **Cloud Storage**: **Supabase Storage** for product and founder pictures
- **Styling**: Bootstrap 5 + FontAwesome icons + Custom CSS
- **Deployment**: Docker, Docker Compose, and GitHub Actions

---

## 🚀 How to Run Locally (Developer Mode)

### Requirements
- [.NET 10 SDK](https://dotnet.microsoft.com/download) installed on your computer.

### Steps
1. **Clone the repository**:
   ```bash
   git clone https://github.com/dubem4521-dot/TJPork.git
   cd TJPork
   ```

2. **Run the project**:
   ```bash
   dotnet run --project src/TJPork.Web
   ```

3. Open your browser and navigate to:
   ```
   http://localhost:5000
   ```
   *(On first run, it automatically creates a local SQLite database and fills it with sample products and an admin account.)*

---

## 🐳 How to Run with Docker (DietPi / Linux Server)

### 1. Create your project folder
```bash
mkdir -p /srv/share/docker/tjpork-app
cd /srv/share/docker/tjpork-app
```

### 2. Create your `.env` file
Create a file named `.env` and fill in your Supabase credentials:

```env
# Supabase PostgreSQL Connection Pooler (Port 5432 or 6543)
DATABASE_URL=postgresql://postgres.YOUR_PROJECT_REF:YOUR_PASSWORD@aws-0-eu-central-1.pooler.supabase.com:5432/postgres

# Supabase Storage for Photos
SUPABASE_URL=https://YOUR_PROJECT_REF.supabase.co
SUPABASE_KEY=YOUR_SUPABASE_SERVICE_ROLE_KEY
SUPABASE_BUCKET=tjpork-images
```

### 3. Create your `docker-compose.yml`
```yaml
services:
  tjpork-web:
    container_name: tjpork-webapp
    image: rustytoothpickk/tjpork-app:latest
    ports:
      - "5000:8080"
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - ASPNETCORE_URLS=http://+:8080
      - DATABASE_URL=${DATABASE_URL}
      - SUPABASE_URL=${SUPABASE_URL}
      - SUPABASE_KEY=${SUPABASE_KEY}
      - SUPABASE_BUCKET=${SUPABASE_BUCKET}
    volumes:
      - ./aspnet-keys:/root/.aspnet/DataProtection-Keys
    restart: unless-stopped
```

### 4. Start the container
```bash
docker compose pull
docker compose up -d
```

Access the store at `http://<your-server-ip>:5000`.

---

## 🔑 Default Accounts (First Startup)

When the application boots up for the first time, it automatically creates default test accounts:

| Role | Email | Password |
|---|---|---|
| **Admin** | `tinashe@tjpork.com` | `Admin2026!#Pork` |
| **Customer** | `max@tjpork.com` | `Customer2026!#Pork` |

*(Be sure to change these passwords after logging in on a live store.)*

---

## 🧪 Running Tests

To verify that all features work properly, run the test suite:

```bash
dotnet test TJPork.sln
```
All tests should pass (24/24 passed).
