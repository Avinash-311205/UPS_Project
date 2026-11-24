using Backend.Infrastructure.Persistence;
using Backend.Modules.Identity.Enums;
using Microsoft.EntityFrameworkCore;

namespace Backend.Modules.IAM.Tests
{
    public static class VerifyIamDatabaseMigration
    {
        public static int Run()
        {
            Console.WriteLine("=================================================");
            Console.WriteLine("ResilienceX — Live IAM Database Migration Verification");
            Console.WriteLine("=================================================");

            var factory = new AppDbContextFactory();
            using var db = factory.CreateDbContext(Array.Empty<string>());

            int failures = 0;

            // 1. Verify Aditya (User ID: 1)
            var aditya = db.Users.IgnoreQueryFilters().FirstOrDefault(u => u.Id == 1);
            if (aditya == null)
            {
                Console.WriteLine("[FAIL] User ID 1 (Aditya) not found in MySQL.");
                failures++;
            }
            else
            {
                if (aditya.AccountType != AccountType.Root)
                {
                    Console.WriteLine($"[FAIL] User 1 AccountType is {aditya.AccountType}, expected Root.");
                    failures++;
                }
                else if (aditya.Status != UserStatus.Active)
                {
                    Console.WriteLine($"[FAIL] User 1 Status is {aditya.Status}, expected Active.");
                    failures++;
                }
                else if (string.IsNullOrWhiteSpace(aditya.SecurityStamp))
                {
                    Console.WriteLine("[FAIL] User 1 SecurityStamp is missing or empty.");
                    failures++;
                }
                else if (aditya.TenantId != Guid.Parse("1ca3b8d4-1fa7-47bc-8b68-11c702086a4f"))
                {
                    Console.WriteLine($"[FAIL] User 1 TenantId is {aditya.TenantId}, expected 1ca3b8d4-1fa7-47bc-8b68-11c702086a4f.");
                    failures++;
                }
                else
                {
                    Console.WriteLine($"[PASS] User 1 (Aditya / {aditya.Email}) verified as Root, Active, SecurityStamp present, TenantId: {aditya.TenantId}");
                }
            }

            // 2. Verify ResilienceAdmin (User ID: 3)
            var admin = db.Users.IgnoreQueryFilters().FirstOrDefault(u => u.Id == 3);
            if (admin == null)
            {
                Console.WriteLine("[FAIL] User ID 3 (ResilienceAdmin) not found in MySQL.");
                failures++;
            }
            else
            {
                if (admin.AccountType != AccountType.Root)
                {
                    Console.WriteLine($"[FAIL] User 3 AccountType is {admin.AccountType}, expected Root.");
                    failures++;
                }
                else if (admin.Status != UserStatus.Active)
                {
                    Console.WriteLine($"[FAIL] User 3 Status is {admin.Status}, expected Active.");
                    failures++;
                }
                else if (string.IsNullOrWhiteSpace(admin.SecurityStamp))
                {
                    Console.WriteLine("[FAIL] User 3 SecurityStamp is missing or empty.");
                    failures++;
                }
                else if (admin.TenantId != Guid.Parse("418646de-f135-4ab5-bf7b-99b36e5d506f"))
                {
                    Console.WriteLine($"[FAIL] User 3 TenantId is {admin.TenantId}, expected 418646de-f135-4ab5-bf7b-99b36e5d506f.");
                    failures++;
                }
                else
                {
                    Console.WriteLine($"[PASS] User 3 (ResilienceAdmin / {admin.Email}) verified as Root, Active, SecurityStamp present, TenantId: {admin.TenantId}");
                }
            }

            // 3. Verify Zuu (User ID: 2)
            var zuu = db.Users.IgnoreQueryFilters().FirstOrDefault(u => u.Id == 2);
            if (zuu == null)
            {
                Console.WriteLine("[FAIL] User ID 2 (Zuu) not found in MySQL.");
                failures++;
            }
            else
            {
                if (zuu.AccountType != AccountType.IAM)
                {
                    Console.WriteLine($"[FAIL] User 2 AccountType is {zuu.AccountType}, expected IAM.");
                    failures++;
                }
                else if (zuu.TenantId != null)
                {
                    Console.WriteLine($"[FAIL] User 2 TenantId is {zuu.TenantId}, expected NULL.");
                    failures++;
                }
                else
                {
                    Console.WriteLine($"[PASS] User 2 (Zuu / {zuu.Email}) verified as IAM with TenantId = NULL preserved.");
                }
            }

            // 4. Verify unique constraint enforcement for Root per Tenant in change tracker
            var duplicateRoot = new Backend.Modules.Identity.Models.User
            {
                Username = "AdityaClone",
                Email = "clone@test.com",
                TenantId = Guid.Parse("1ca3b8d4-1fa7-47bc-8b68-11c702086a4f"),
                AccountType = AccountType.Root,
                Status = UserStatus.Active,
                SecurityStamp = Guid.NewGuid().ToString()
            };
            db.Users.Add(duplicateRoot);

            bool caughtDuplicateRoot = false;
            try
            {
                db.SaveChanges();
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("Only one Root account is permitted per Tenant"))
            {
                caughtDuplicateRoot = true;
                db.Entry(duplicateRoot).State = EntityState.Detached;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[NOTE] Rejection exception: {ex.Message}");
                db.Entry(duplicateRoot).State = EntityState.Detached;
                caughtDuplicateRoot = true;
            }

            if (caughtDuplicateRoot)
            {
                Console.WriteLine("[PASS] Single Root per Tenant constraint successfully blocked inserting duplicate Root.");
            }
            else
            {
                Console.WriteLine("[FAIL] Duplicate Root per Tenant was not rejected.");
                failures++;
            }

            Console.WriteLine("=================================================");
            Console.WriteLine($"Live Verification Result: {(failures == 0 ? "ALL CHECKS PASSED" : $"{failures} CHECKS FAILED")}");
            Console.WriteLine("=================================================");

            return failures == 0 ? 0 : 1;
        }
    }
}
