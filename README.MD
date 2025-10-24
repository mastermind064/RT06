# Sistem Informasi RT Multi-Tenant

Implementasi backend awal berdasarkan rancangan ini tersedia di folder `src/RTMultiTenant.Api`. Proyek ASP.NET Core 8 tersebut
menyediakan:

- Endpoint otentikasi JWT dengan dukungan multi-RT (tenant) berbasis klaim `rt_id`.
- Event sourcing sederhana menggunakan tabel `event_store` serta proyeksi read-model untuk warga, iuran, dan kas.
- Fitur utama sesuai blueprint: pengajuan profil warga, persetujuan admin, pelaporan iuran, pencatatan pengeluaran kas, dan
  ringkasan kas bulanan multi-tenant.

Lihat bagian **Panduan Menjalankan Backend** di akhir dokumen untuk detail build dan konfigurasi.

Frontend Angular yang memanfaatkan API di atas dapat ditemukan di folder `src/rt-frontend` dengan fitur utama:

- Portal login JWT multi-RT serta penyimpanan token di `localStorage`.
- Dashboard ringkasan kas bulanan dengan filter tahun/bulan dan tampilan kartu Bootstrap.
- Formulir profil warga, pengajuan iuran, dan daftar iuran dengan aksi persetujuan admin.
- Modul admin untuk mengelola pengeluaran kas serta aktivasi/nonaktifasi akun pengguna.
- Styling Bootstrap 5 dan layout responsif berbasis komponen standalone Angular 17.

Instruksi build dan pengembangan frontend tersedia pada bagian **Panduan Menjalankan Frontend**.

## 1. Gambaran Umum Sistem

Tujuan sistem:

- Membantu banyak RT sekaligus dalam satu aplikasi.
- Mengelola data warga, keluarga, dan iuran kas.
- Membantu admin RT mencatat kas keluar dan memvalidasi kas masuk.
- Menyajikan laporan kas bulanan ke warga secara transparan.

Prinsip penting:

- **Multi-RT**: setiap RT adalah "tenant". Data antar RT harus terpisah.
- **Event Sourcing**: semua perubahan penting dicatat sebagai event immutable.
- **Read Model / State**: data terakhir disimpan dalam bentuk tabel terstruktur agar cepat ditampilkan (tanpa replay event di runtime API/Angular).

## 2. Fitur per Role

### Role: Warga

- Melihat/melengkapi profil dirinya dan anggota keluarga (istri/anak).
- Mengunggah kartu keluarga (KK).
- Mengajukan atau memperbarui data keluarga (status **PENDING** sampai admin menyetujui).
- Melaporkan pembayaran iuran RT (satu atau banyak bulan sekaligus, termasuk satu tahun).
  - Mengunggah bukti bayar (foto/transfer).
  - Masih bisa diubah selama status belum **APPROVED**.
- Melihat status pembayaran iuran miliknya.
- Melihat ringkasan kas RT bulanan (uang masuk & keluar).

### Role: Admin RT

- Menyetujui atau menolak data kependudukan warga (profil keluarga + KK).
- Menyetujui atau menolak laporan iuran dari warga.
- Menginput dan mengedit pengeluaran kas RT (catatan pemakaian dana kas).
- Melihat dashboard kas bulanan (pemasukan, pengeluaran, saldo).
- Membuat dan mengelola akun user (membuat user baru, aktif/nonaktifkan, ubah role dalam RT yang sama).

**Scope Data**

- Admin RT 01 hanya bisa mengelola data RT 01.
- Warga RT 01 hanya bisa melihat data RT 01.
- Tidak ada akses silang antar RT; kontrol dilakukan menggunakan `rt_id`.

## 3. Arsitektur Teknis

- **Backend**: .NET Core 8 Web API (RESTful)
- **Frontend**: Angular
- **Database**: MySQL
- **Auth**: JWT bearer token

### Alur Autentikasi

1. User login:
   - Backend memvalidasi username/password.
   - Backend mengambil `user_id`, `role`, `rt_id`.
   - Backend mengirim JWT berisi klaim `sub` (user_id), `role`, dan `rt_id`.
2. Setiap request berikutnya:
   - Backend mengekstrak `rt_id` dari JWT.
   - Semua query/INSERT/UPDATE menggunakan `rt_id` dari JWT, bukan dari body request.
   - Menjamin user tidak bisa berpura-pura menjadi RT lain.

### Pola Command / Query

- **Command**: validasi role & `rt_id`, tulis event ke `event_store`, perbarui tabel state/read model (proyeksi), kemudian kembalikan hasil.
- **Query**: langsung `SELECT` dari tabel state/read model (difilter `rt_id`) sehingga respons cepat tanpa replay event.

## 4. Event Sourcing

Aggregate utama:

1. **ResidentAggregate**
   - Mengelola data profil warga dan keluarganya.
   - Event contoh: `ResidentProfileSubmitted`, `ResidentProfileApproved/Rejected`, `FamilyMemberAdded/Updated/Removed`, `KKDocumentUploaded`.
2. **ContributionAggregate**
   - Mengelola laporan pembayaran iuran kas warga.
   - Event contoh: `ContributionReported`, `ContributionUpdated`, `ContributionApproved/Rejected`.
3. **CashFlowAggregate**
   - Mengelola pengeluaran kas RT.
   - Event contoh: `ExpenseRecorded`, `ExpenseUpdated`, `ExpenseDeleted`, `InitialBalanceSet` (opsional).

Semua event disimpan immutable di `event_store`, mencatat pelaku (`caused_by_user_id`), waktu, dan `rt_id`. Setiap event diproyeksikan ke tabel state yang query-friendly: `residents`, `resident_family_members`, `contributions`, `cash_expenses`, `monthly_cash_summary`.

Manfaat:

- Audit trail komprehensif.
- Riwayat perubahan dapat ditelusuri untuk penyelesaian sengketa.
- Transparansi transaksi kas RT.

## 5. Desain Tabel MySQL (Final)

Catatan umum:

- Semua primary key domain menggunakan `CHAR(32)` (GUID tanpa dash).
- Semua tabel domain menyertakan `rt_id`.
- Indeks/foreign key menggunakan `rt_id` untuk memastikan isolasi tenant.
- Timestamp menggunakan `DATETIME` dalam zona waktu konsisten (misal UTC).

### 5.1 Tabel `rt`
```sql
CREATE TABLE rt (
    rt_id              CHAR(32) PRIMARY KEY,
    rt_number          VARCHAR(10) NOT NULL,
    rw_number          VARCHAR(10) NOT NULL,
    village_name       VARCHAR(100) NOT NULL,
    subdistrict_name   VARCHAR(100) NOT NULL,
    city_name          VARCHAR(100) NOT NULL,
    province_name      VARCHAR(100) NOT NULL,
    address_detail     VARCHAR(255) NULL,
    created_at         DATETIME NOT NULL,
    updated_at         DATETIME NOT NULL,
    UNIQUE KEY uq_rt_context (
        rt_number,
        rw_number,
        village_name,
        subdistrict_name,
        city_name,
        province_name
    )
);
```

### 5.2 Tabel `users`
```sql
CREATE TABLE users (
    user_id            CHAR(32) PRIMARY KEY,
    rt_id              CHAR(32) NOT NULL,
    username           VARCHAR(50) NOT NULL UNIQUE,
    password_hash      VARCHAR(255) NOT NULL,
    role               ENUM('ADMIN', 'WARGA') NOT NULL DEFAULT 'WARGA',
    is_active          TINYINT(1) NOT NULL DEFAULT 1,
    resident_id        CHAR(32) NULL,
    created_at         DATETIME NOT NULL,
    updated_at         DATETIME NOT NULL,
    CONSTRAINT fk_user_rt
        FOREIGN KEY (rt_id) REFERENCES rt(rt_id)
);
```

### 5.3 Tabel `event_store`
```sql
CREATE TABLE event_store (
    event_id           BIGINT AUTO_INCREMENT PRIMARY KEY,
    rt_id              CHAR(32) NOT NULL,
    aggregate_id       CHAR(32) NOT NULL,
    aggregate_type     ENUM('RESIDENT','CONTRIBUTION','CASHFLOW') NOT NULL,
    event_type         VARCHAR(100) NOT NULL,
    event_payload      JSON NOT NULL,
    occurred_at        DATETIME NOT NULL,
    caused_by_user_id  CHAR(32) NOT NULL,
    aggregate_version  INT NOT NULL,
    INDEX idx_rt_agg_ver (rt_id, aggregate_id, aggregate_version),
    INDEX idx_rt_type_time (rt_id, aggregate_type, occurred_at),
    INDEX idx_rt_user_time (rt_id, caused_by_user_id, occurred_at),
    CONSTRAINT fk_event_rt
        FOREIGN KEY (rt_id) REFERENCES rt(rt_id),
    CONSTRAINT fk_event_user
        FOREIGN KEY (caused_by_user_id) REFERENCES users(user_id)
);
```

### 5.4 Tabel `residents`
```sql
CREATE TABLE residents (
    resident_id            CHAR(32) PRIMARY KEY,
    rt_id                  CHAR(32) NOT NULL,
    national_id_number     VARCHAR(32) NOT NULL,
    full_name              VARCHAR(100) NOT NULL,
    birth_date             DATE NOT NULL,
    gender                 ENUM('L','P') NOT NULL,
    address                VARCHAR(255) NOT NULL,
    phone_number           VARCHAR(30) NOT NULL,
    kk_document_path       VARCHAR(255) NULL,
    approval_status        ENUM('DRAFT','PENDING','APPROVED','REJECTED')
                          NOT NULL DEFAULT 'DRAFT',
    approval_note          VARCHAR(255) NULL,
    created_at             DATETIME NOT NULL,
    updated_at             DATETIME NOT NULL,
    INDEX idx_rt_resident (rt_id),
    INDEX idx_rt_status (rt_id, approval_status),
    CONSTRAINT fk_resident_rt
        FOREIGN KEY (rt_id) REFERENCES rt(rt_id)
);
```

### 5.5 Tabel `resident_family_members`
```sql
CREATE TABLE resident_family_members (
    family_member_id       CHAR(32) PRIMARY KEY,
    rt_id                  CHAR(32) NOT NULL,
    resident_id            CHAR(32) NOT NULL,
    full_name              VARCHAR(100) NOT NULL,
    birth_date             DATE NOT NULL,
    gender                 ENUM('L','P') NOT NULL,
    relationship           ENUM('ISTRI','SUAMI','ANAK','LAINNYA') NOT NULL,
    created_at             DATETIME NOT NULL,
    updated_at             DATETIME NOT NULL,
    INDEX idx_rt_family (rt_id, resident_id),
    CONSTRAINT fk_family_resident
        FOREIGN KEY (resident_id) REFERENCES residents(resident_id),
    CONSTRAINT fk_family_rt
        FOREIGN KEY (rt_id) REFERENCES rt(rt_id)
);
```

### 5.6 Tabel `contributions`
```sql
CREATE TABLE contributions (
    contribution_id        CHAR(32) PRIMARY KEY,
    rt_id                  CHAR(32) NOT NULL,
    resident_id            CHAR(32) NOT NULL,
    period_start           DATE NOT NULL,
    period_end             DATE NOT NULL,
    amount_paid            DECIMAL(14,2) NOT NULL,
    payment_date           DATE NOT NULL,
    proof_image_path       VARCHAR(255) NOT NULL,
    status                 ENUM('DRAFT','PENDING','APPROVED','REJECTED')
                          NOT NULL DEFAULT 'PENDING',
    admin_note             VARCHAR(255) NULL,
    created_at             DATETIME NOT NULL,
    updated_at             DATETIME NOT NULL,
    INDEX idx_rt_contrib (rt_id),
    INDEX idx_rt_resident (rt_id, resident_id),
    INDEX idx_rt_status (rt_id, status),
    INDEX idx_rt_period (rt_id, period_start, period_end),
    CONSTRAINT fk_contrib_rt
        FOREIGN KEY (rt_id) REFERENCES rt(rt_id),
    CONSTRAINT fk_contrib_resident
        FOREIGN KEY (resident_id) REFERENCES residents(resident_id)
);
```

### 5.7 Tabel `cash_expenses`
```sql
CREATE TABLE cash_expenses (
    expense_id             CHAR(32) PRIMARY KEY,
    rt_id                  CHAR(32) NOT NULL,
    expense_date           DATE NOT NULL,
    description            VARCHAR(255) NOT NULL,
    amount                 DECIMAL(14,2) NOT NULL,
    created_by_user_id     CHAR(32) NOT NULL,
    is_active              TINYINT(1) NOT NULL DEFAULT 1,
    created_at             DATETIME NOT NULL,
    updated_at             DATETIME NOT NULL,
    INDEX idx_rt_expense_date (rt_id, expense_date),
    INDEX idx_rt_active (rt_id, is_active),
    CONSTRAINT fk_expense_rt
        FOREIGN KEY (rt_id) REFERENCES rt(rt_id),
    CONSTRAINT fk_expense_user
        FOREIGN KEY (created_by_user_id) REFERENCES users(user_id)
);
```

### 5.8 Tabel `monthly_cash_summary`
```sql
CREATE TABLE monthly_cash_summary (
    summary_id             CHAR(32) PRIMARY KEY,
    rt_id                  CHAR(32) NOT NULL,
    year                   INT NOT NULL,
    month                  INT NOT NULL,
    total_contribution_in  DECIMAL(14,2) NOT NULL DEFAULT 0.00,
    total_expense_out      DECIMAL(14,2) NOT NULL DEFAULT 0.00,
    balance_end            DECIMAL(14,2) NOT NULL DEFAULT 0.00,
    generated_at           DATETIME NOT NULL,
    UNIQUE KEY uq_rt_year_month (rt_id, year, month),
    CONSTRAINT fk_summary_rt
        FOREIGN KEY (rt_id) REFERENCES rt(rt_id)
);
```

Catatan saldo:

- `balance_end` bulan X = `balance_end` bulan sebelumnya + pemasukan bulan X - pengeluaran bulan X.
- Kontribusi berstatus `APPROVED` menambah `total_contribution_in` pada bulan `payment_date`.
- Pengeluaran aktif menambah `total_expense_out` pada bulan `expense_date`.
- Re-kalkulasi maju per RT menjaga konsistensi saldo.

## 6. Alur Utama (Use Case Flow)

1. **Warga memperbarui profil keluarga**: event `ResidentProfileSubmitted` dan `FamilyMemberAdded` menyimpan status `PENDING`; admin menyetujui melalui event `ResidentProfileApproved`.
2. **Warga melapor iuran kas**: event `ContributionReported` membuat catatan `PENDING` di `contributions`.
3. **Warga mengedit iuran sebelum disetujui**: event `ContributionUpdated` memperbarui catatan yang sama selama status belum `APPROVED`.
4. **Admin menyetujui iuran**: event `ContributionApproved` mengubah status menjadi `APPROVED` dan memperbarui `monthly_cash_summary` sesuai `payment_date`.
5. **Admin mencatat pengeluaran kas**: event `ExpenseRecorded` menambah `cash_expenses` dan memperbarui `monthly_cash_summary` berdasarkan `expense_date`.
6. **Warga melihat laporan kas bulanan**: frontend memanggil endpoint summary, backend mem-filter berdasarkan `rt_id` dari JWT dan mengembalikan ringkasan pemasukan, pengeluaran, dan saldo.

## 7. ERD (ASCII)

```
+---------------------+
|        rt           |
+---------------------+
| rt_id (PK)          |
| rt_number           |
| rw_number           |
| ...                 |
+----------+----------+
           |
           | 1
           |
           | n
           v
+---------------------+
|       users         |
+---------------------+
| user_id (PK)        |
| rt_id (FK->rt)      |
| username            |
| password_hash       |
| role                |
| resident_id (FK)    |
+----------+----------+
           |
           | 1 user (admin)
           | n expense/event
           v
+---------------------+
|    cash_expenses    |
+---------------------+
| expense_id (PK)     |
| rt_id (FK->rt)      |
| created_by_user_id  |
| expense_date        |
| amount              |
| is_active           |
+----------+----------+

           rt_id
   1 RT  --------- n Residents

+---------------------+
|     residents       |
+---------------------+
| resident_id (PK)    |
| rt_id (FK->rt)      |
| full_name           |
| NIK                 |
| approval_status     |
| kk_document_path    |
+----------+----------+
           |
           | 1 resident
           | n anggota keluarga
           v
+-------------------------------+
|  resident_family_members      |
+-------------------------------+
| family_member_id (PK)         |
| rt_id (FK->rt)                |
| resident_id (FK->residents)   |
| full_name                     |
| relationship                  |
+-------------------------------+

           |
           | 1 resident
           | n contributions
           v
+---------------------+
|    contributions    |
+---------------------+
| contribution_id (PK)|
| rt_id (FK->rt)      |
| resident_id (FK)    |
| period_start        |
| period_end          |
| amount_paid         |
| payment_date        |
| proof_image_path    |
| status              |
+---------------------+

rt_id
 1 RT
  n monthly summaries

+----------------------------+
|   monthly_cash_summary     |
+----------------------------+
| summary_id (PK)            |
| rt_id (FK->rt)             |
| year                       |
| month                      |
| total_contribution_in      |
| total_expense_out          |
| balance_end                |
+----------------------------+

Event sourcing (audit trail global):

+---------------------+
|     event_store     |
+---------------------+
| event_id (PK, AI)   |
| rt_id (FK->rt)      |
| aggregate_id        |
| aggregate_type      |
| event_type          |
| event_payload (JSON)|
| occurred_at         |
| caused_by_user_id   |
| aggregate_version   |
+---------------------+
```

## Panduan Menjalankan Backend

1. **Persiapkan lingkungan**
   - Pastikan .NET SDK 8.0 dan MySQL 8 tersedia.
   - Buat database kosong sesuai string koneksi default (`rt_multi_tenant`) atau ubah `ConnectionStrings:DefaultConnection` pada
     `src/RTMultiTenant.Api/appsettings.json`.

2. **Restorasi dependensi**
   ```bash
   dotnet restore RTMultiTenant.sln
   ```

3. **Jalankan migrasi** (opsional – struktur entitas sudah disesuaikan dengan tabel pada blueprint, sehingga migrasi dapat
   dihasilkan dengan `dotnet ef migrations add InitialCreate`).

4. **Menjalankan API**
   ```bash
   dotnet run --project src/RTMultiTenant.Api/RTMultiTenant.Api.csproj
   ```

5. **Alur bootstrap**
   - Panggil `POST /api/rts` untuk membuat tenant baru sekaligus admin pertama.
   - Gunakan token JWT hasil respons untuk mengakses endpoint admin (`Authorization: Bearer <token>`).
   - Admin dapat membuat akun warga via `POST /api/auth/register`; warga login dan mengelola profil, iuran, dan melihat ringkasan
     kas sesuai RT masing-masing.

## Panduan Menjalankan Frontend

1. **Instal dependensi Node.js**
   - Pastikan Node.js 18+ dan npm tersedia.

2. **Instal dependensi proyek**
   ```bash
   cd src/rt-frontend
   npm install
   ```

3. **Menjalankan aplikasi pengembangan**
   ```bash
   npm start
   ```
   - Aplikasi akan tersedia pada `http://localhost:4200`.
   - Pastikan variabel `environment.apiUrl` mengarah ke alamat backend yang sedang berjalan.

4. **Build produksi**
   ```bash
   npm run build
   ```
   - Artefak build akan berada di `dist/rt-frontend` dan dapat disajikan oleh web server statis.

## 8. Langkah Lanjut

- Generate migrasi MySQL berdasarkan skema di atas.
- Implementasi model/DTO dan proyeksi event di .NET Core 8.
- Menambahkan guard serta interceptor auth berbasis role dan `rt_id` di Angular.
- Membangun antarmuka approval untuk admin dan ringkasan kas untuk warga.
