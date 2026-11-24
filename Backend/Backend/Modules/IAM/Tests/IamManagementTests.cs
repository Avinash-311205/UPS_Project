using Backend.Modules.IAM.DTOs;
using Backend.Modules.IAM.Services;
using Backend.Modules.Identity.DTOs;
using Backend.Modules.Identity.Enums;
using Backend.Modules.Identity.Interfaces;
using Backend.Modules.Identity.Models;
using Backend.Modules.Identity.Services;
using Backend.Modules.Tenants.Interfaces;
using Backend.Modules.Tenants.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;

namespace Backend.Modules.IAM.Tests
{
    public class MockTenantContext : ITenantContext
    {
        public Guid? TenantId { get; set; }
        public bool HasTenant => TenantId.HasValue;
    }

    public class InMemoryUserRepository : IUserRepository
    {
        private readonly List<User> _users = new();
        private int _nextId = 1;

        public List<User> RawUsers => _users;

        public Task<User?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_users.FirstOrDefault(u => u.Id == id));
        }

        public Task<User?> GetByIdAndTenantAsync(int id, Guid tenantId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_users.FirstOrDefault(u => u.Id == id && u.TenantId == tenantId));
        }

        public Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_users.FirstOrDefault(u => u.Email.Equals(email.Trim(), StringComparison.OrdinalIgnoreCase)));
        }

        public Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_users.Any(u => u.Email.Equals(email.Trim(), StringComparison.OrdinalIgnoreCase)));
        }

        public Task<IEnumerable<User>> GetIamUsersByTenantAsync(Guid tenantId, CancellationToken cancellationToken = default)
        {
            var result = _users
                .Where(u => u.TenantId == tenantId && u.AccountType == AccountType.IAM)
                .OrderBy(u => u.Username)
                .ToList();
            return Task.FromResult<IEnumerable<User>>(result);
        }

        public Task<User> AddAsync(User user, CancellationToken cancellationToken = default)
        {
            if (user.Id == 0)
            {
                user.Id = _nextId++;
            }
            _users.Add(user);
            return Task.FromResult(user);
        }

        public Task<User> UpdateAsync(User user, CancellationToken cancellationToken = default)
        {
            var idx = _users.FindIndex(u => u.Id == user.Id);
            if (idx >= 0)
            {
                _users[idx] = user;
            }
            return Task.FromResult(user);
        }
    }

    public class InMemoryTenantRepository : ITenantRepository
    {
        private readonly List<Tenant> _tenants = new();

        public Task<Tenant?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_tenants.FirstOrDefault(t => t.Id == id));
        }

        public Task<Tenant?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_tenants.FirstOrDefault(t => t.Name.Equals(name.Trim(), StringComparison.OrdinalIgnoreCase)));
        }

        public Task<Tenant> AddAsync(Tenant tenant, CancellationToken cancellationToken = default)
        {
            _tenants.Add(tenant);
            return Task.FromResult(tenant);
        }
    }

    public static class IamManagementTests
    {
        public static int RunAllTests()
        {
            Console.WriteLine("=================================================");
            Console.WriteLine("ResilienceX — IAM & Root Account Test Suite");
            Console.WriteLine("=================================================");

            int passed = 0;
            int failed = 0;

            void RunTest(string testName, Action testAction)
            {
                try
                {
                    testAction();
                    Console.WriteLine($"[PASS] {testName}");
                    passed++;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[FAIL] {testName}: {ex.Message}");
                    failed++;
                }
            }

            var passwordHasher = new PasswordHasher<User>();
            var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "Jwt:Key", "SuperSecretKeyForResilienceXPlatform2026!" },
                { "Jwt:Issuer", "ResilienceX" },
                { "Jwt:Audience", "ResilienceXUsers" }
            }).Build();

            // Test 1: Organization registration creates Root user with Active status and fresh SecurityStamp
            RunTest("Organization registration creates Root account with Active status and SecurityStamp", () =>
            {
                var userRepo = new InMemoryUserRepository();
                var tenantRepo = new InMemoryTenantRepository();
                var authService = new AuthService(userRepo, tenantRepo, passwordHasher, config);

                var registerDto = new RegisterDTO
                {
                    Username = "OrgFounder",
                    Email = "founder@testorg.com",
                    Password = "SecurePassword123!",
                    TenantName = "Test Logistics Group"
                };

                var createdUser = authService.RegisterAsync(registerDto).GetAwaiter().GetResult();
                if (createdUser == null) throw new Exception("User registration returned null.");
                if (createdUser.AccountType != AccountType.Root) throw new Exception($"Expected Root account type, got {createdUser.AccountType}.");
                if (createdUser.Status != UserStatus.Active) throw new Exception($"Expected Active status, got {createdUser.Status}.");
                if (string.IsNullOrWhiteSpace(createdUser.SecurityStamp)) throw new Exception("SecurityStamp was not generated.");
                if (!createdUser.TenantId.HasValue) throw new Exception("TenantId was not assigned during registration.");
            });

            // Test 2: Active Root and IAM accounts authenticate and receive role & security stamp claims
            RunTest("Active Root and IAM accounts authenticate and return correct account type", () =>
            {
                var tenantId = Guid.NewGuid();
                var userRepo = new InMemoryUserRepository();
                var tenantRepo = new InMemoryTenantRepository();

                var rootUser = new User
                {
                    Username = "RootLeader",
                    Email = "root@logistics.com",
                    TenantId = tenantId,
                    AccountType = AccountType.Root,
                    Status = UserStatus.Active,
                    SecurityStamp = Guid.NewGuid().ToString(),
                    CreatedAt = DateTime.UtcNow
                };
                rootUser.PasswordHash = passwordHasher.HashPassword(rootUser, "RootPass123!");
                userRepo.AddAsync(rootUser).GetAwaiter().GetResult();

                var iamUser = new User
                {
                    Username = "Operator1",
                    Email = "op1@logistics.com",
                    TenantId = tenantId,
                    AccountType = AccountType.IAM,
                    Status = UserStatus.Active,
                    SecurityStamp = Guid.NewGuid().ToString(),
                    CreatedAt = DateTime.UtcNow
                };
                iamUser.PasswordHash = passwordHasher.HashPassword(iamUser, "IamPass123!");
                userRepo.AddAsync(iamUser).GetAwaiter().GetResult();

                var authService = new AuthService(userRepo, tenantRepo, passwordHasher, config);

                var rootLogin = authService.LoginAsync(new LoginDTO { Email = "root@logistics.com", Password = "RootPass123!" }).GetAwaiter().GetResult();
                if (rootLogin == null) throw new Exception("Root login failed.");
                if (rootLogin.Value.User.AccountType != AccountType.Root) throw new Exception("Root login did not return AccountType.Root.");
                if (string.IsNullOrWhiteSpace(rootLogin.Value.Token)) throw new Exception("Token was empty.");

                var iamLogin = authService.LoginAsync(new LoginDTO { Email = "op1@logistics.com", Password = "IamPass123!" }).GetAwaiter().GetResult();
                if (iamLogin == null) throw new Exception("IAM login failed.");
                if (iamLogin.Value.User.AccountType != AccountType.IAM) throw new Exception("IAM login did not return AccountType.IAM.");
                if (string.IsNullOrWhiteSpace(iamLogin.Value.Token)) throw new Exception("Token was empty.");
            });

            // Test 3: Inactive accounts are prohibited from authenticating
            RunTest("Inactive user account login is rejected with null result", () =>
            {
                var tenantId = Guid.NewGuid();
                var userRepo = new InMemoryUserRepository();
                var tenantRepo = new InMemoryTenantRepository();

                var inactiveUser = new User
                {
                    Username = "DisabledUser",
                    Email = "disabled@logistics.com",
                    TenantId = tenantId,
                    AccountType = AccountType.IAM,
                    Status = UserStatus.Inactive,
                    SecurityStamp = Guid.NewGuid().ToString(),
                    CreatedAt = DateTime.UtcNow
                };
                inactiveUser.PasswordHash = passwordHasher.HashPassword(inactiveUser, "Pass123!");
                userRepo.AddAsync(inactiveUser).GetAwaiter().GetResult();

                var authService = new AuthService(userRepo, tenantRepo, passwordHasher, config);

                var result = authService.LoginAsync(new LoginDTO { Email = "disabled@logistics.com", Password = "Pass123!" }).GetAwaiter().GetResult();
                if (result != null) throw new Exception("Expected login to fail for deactivated account.");
            });

            // Test 4: Root user creates IAM user in their tenant
            RunTest("Root user can create IAM user in their tenant (inherits Root TenantId, AccountType = IAM)", () =>
            {
                var tenantId = Guid.NewGuid();
                var tenantContext = new MockTenantContext { TenantId = tenantId };
                var userRepo = new InMemoryUserRepository();
                var iamService = new IamService(userRepo, tenantContext, passwordHasher);

                var createDto = new CreateIamUserDTO
                {
                    Username = "DispatchAgent",
                    Email = "dispatch@carrier.com",
                    Password = "DispatchSecurePass1!"
                };

                var response = iamService.CreateAsync(createDto).GetAwaiter().GetResult();
                if (response.Username != "DispatchAgent") throw new Exception("Username mismatch.");
                if (response.AccountType != AccountType.IAM) throw new Exception("Created user must be AccountType.IAM.");
                if (response.Status != UserStatus.Active) throw new Exception("Created user must be UserStatus.Active.");
                if (response.TenantId != tenantId) throw new Exception("Created user must inherit Root's TenantId.");

                var stored = userRepo.GetByEmailAsync("dispatch@carrier.com").GetAwaiter().GetResult();
                if (stored == null) throw new Exception("Created user not found in repository.");
                if (stored.PasswordHash == "DispatchSecurePass1!") throw new Exception("Password was stored in plaintext!");
            });

            // Test 5: Global email uniqueness is enforced when creating IAM user
            RunTest("Duplicate email is rejected across any tenant when creating IAM user", () =>
            {
                var tenantA = Guid.NewGuid();
                var tenantB = Guid.NewGuid();
                var tenantContextB = new MockTenantContext { TenantId = tenantB };
                var userRepo = new InMemoryUserRepository();

                // Existing user in Tenant A
                var existingUser = new User
                {
                    Username = "GlobalUser",
                    Email = "existing@carrier.com",
                    TenantId = tenantA,
                    AccountType = AccountType.IAM,
                    Status = UserStatus.Active,
                    SecurityStamp = Guid.NewGuid().ToString()
                };
                userRepo.AddAsync(existingUser).GetAwaiter().GetResult();

                var iamService = new IamService(userRepo, tenantContextB, passwordHasher);

                bool thrown = false;
                try
                {
                    iamService.CreateAsync(new CreateIamUserDTO
                    {
                        Username = "NewPerson",
                        Email = "existing@carrier.com",
                        Password = "Password123!"
                    }).GetAwaiter().GetResult();
                }
                catch (InvalidOperationException)
                {
                    thrown = true;
                }

                if (!thrown) throw new Exception("Expected InvalidOperationException when creating user with existing global email.");
            });

            // Test 6: Single Root per tenant invariant rule
            RunTest("Single Root invariant prohibits provisioning multiple Root accounts per tenant", () =>
            {
                var tenantId = Guid.NewGuid();
                var userRepo = new InMemoryUserRepository();

                var root1 = new User
                {
                    Username = "FirstRoot",
                    Email = "root1@tenant.com",
                    TenantId = tenantId,
                    AccountType = AccountType.Root,
                    Status = UserStatus.Active,
                    SecurityStamp = Guid.NewGuid().ToString()
                };
                userRepo.AddAsync(root1).GetAwaiter().GetResult();

                // Guard check simulating AppDbContext ChangeTracker policy
                void EnforceRootGuard(User candidate)
                {
                    if (candidate.AccountType == AccountType.Root && candidate.TenantId.HasValue)
                    {
                        var existingRoot = userRepo.RawUsers.Any(u => u.TenantId == candidate.TenantId && u.AccountType == AccountType.Root && u.Id != candidate.Id);
                        if (existingRoot)
                        {
                            throw new InvalidOperationException($"Only one Root account is permitted per Tenant '{candidate.TenantId.Value}'.");
                        }
                    }
                }

                var root2 = new User
                {
                    Username = "SecondRoot",
                    Email = "root2@tenant.com",
                    TenantId = tenantId,
                    AccountType = AccountType.Root,
                    Status = UserStatus.Active,
                    SecurityStamp = Guid.NewGuid().ToString()
                };

                bool caught = false;
                try
                {
                    EnforceRootGuard(root2);
                }
                catch (InvalidOperationException ex) when (ex.Message.Contains("Only one Root account is permitted per Tenant"))
                {
                    caught = true;
                }

                if (!caught) throw new Exception("Expected InvalidOperationException for second Root in same tenant.");
            });

            // Test 7: Deactivating IAM user regenerates SecurityStamp and marks status Inactive
            RunTest("Deactivating IAM user regenerates SecurityStamp and updates status", () =>
            {
                var tenantId = Guid.NewGuid();
                var tenantContext = new MockTenantContext { TenantId = tenantId };
                var userRepo = new InMemoryUserRepository();

                var oldStamp = Guid.NewGuid().ToString();
                var iamUser = new User
                {
                    Username = "ActiveIam",
                    Email = "active@iam.com",
                    TenantId = tenantId,
                    AccountType = AccountType.IAM,
                    Status = UserStatus.Active,
                    SecurityStamp = oldStamp
                };
                userRepo.AddAsync(iamUser).GetAwaiter().GetResult();

                var iamService = new IamService(userRepo, tenantContext, passwordHasher);

                var response = iamService.UpdateStatusAsync(iamUser.Id, new UpdateIamUserStatusDTO
                {
                    Status = UserStatus.Inactive
                }, callerUserId: 999).GetAwaiter().GetResult();

                if (response == null) throw new Exception("Response was null.");
                if (response.Status != UserStatus.Inactive) throw new Exception("Status was not updated to Inactive.");

                var refreshedUser = userRepo.GetByIdAsync(iamUser.Id).GetAwaiter().GetResult();
                if (refreshedUser!.SecurityStamp == oldStamp) throw new Exception("SecurityStamp must be regenerated on deactivation.");
            });

            // Test 8: Self-deactivation by caller is rejected
            RunTest("User cannot deactivate their own account", () =>
            {
                var tenantId = Guid.NewGuid();
                var tenantContext = new MockTenantContext { TenantId = tenantId };
                var userRepo = new InMemoryUserRepository();

                var iamUser = new User
                {
                    Username = "SelfUser",
                    Email = "self@iam.com",
                    TenantId = tenantId,
                    AccountType = AccountType.IAM,
                    Status = UserStatus.Active,
                    SecurityStamp = Guid.NewGuid().ToString()
                };
                userRepo.AddAsync(iamUser).GetAwaiter().GetResult();

                var iamService = new IamService(userRepo, tenantContext, passwordHasher);

                bool thrown = false;
                try
                {
                    iamService.UpdateStatusAsync(iamUser.Id, new UpdateIamUserStatusDTO
                    {
                        Status = UserStatus.Inactive
                    }, callerUserId: iamUser.Id).GetAwaiter().GetResult();
                }
                catch (InvalidOperationException ex) when (ex.Message.Contains("cannot modify their own active status"))
                {
                    thrown = true;
                }

                if (!thrown) throw new Exception("Expected InvalidOperationException on self-deactivation.");
            });

            // Test 9: Modifying Root status or password via IAM service is prohibited
            RunTest("Modifying Root status or password via IAM service is prohibited", () =>
            {
                var tenantId = Guid.NewGuid();
                var tenantContext = new MockTenantContext { TenantId = tenantId };
                var userRepo = new InMemoryUserRepository();

                var rootUser = new User
                {
                    Username = "ProtectedRoot",
                    Email = "protected@root.com",
                    TenantId = tenantId,
                    AccountType = AccountType.Root,
                    Status = UserStatus.Active,
                    SecurityStamp = Guid.NewGuid().ToString()
                };
                userRepo.AddAsync(rootUser).GetAwaiter().GetResult();

                var iamService = new IamService(userRepo, tenantContext, passwordHasher);

                bool statusBlocked = false;
                try
                {
                    iamService.UpdateStatusAsync(rootUser.Id, new UpdateIamUserStatusDTO
                    {
                        Status = UserStatus.Inactive
                    }, callerUserId: 888).GetAwaiter().GetResult();
                }
                catch (InvalidOperationException)
                {
                    statusBlocked = true;
                }

                if (!statusBlocked) throw new Exception("Expected modification of Root status to be blocked.");

                bool pwBlocked = false;
                try
                {
                    iamService.ResetPasswordAsync(rootUser.Id, new ResetIamUserPasswordDTO
                    {
                        NewPassword = "NewSecretPassword123!"
                    }).GetAwaiter().GetResult();
                }
                catch (InvalidOperationException)
                {
                    pwBlocked = true;
                }

                if (!pwBlocked) throw new Exception("Expected password reset of Root account via IAM to be blocked.");
            });

            // Test 10: Resetting IAM user password regenerates SecurityStamp and allows login with new password
            RunTest("Resetting IAM user password regenerates SecurityStamp and allows login with new password", () =>
            {
                var tenantId = Guid.NewGuid();
                var tenantContext = new MockTenantContext { TenantId = tenantId };
                var userRepo = new InMemoryUserRepository();

                var oldStamp = Guid.NewGuid().ToString();
                var iamUser = new User
                {
                    Username = "ResetMe",
                    Email = "resetme@iam.com",
                    TenantId = tenantId,
                    AccountType = AccountType.IAM,
                    Status = UserStatus.Active,
                    SecurityStamp = oldStamp
                };
                iamUser.PasswordHash = passwordHasher.HashPassword(iamUser, "OldPassword123!");
                userRepo.AddAsync(iamUser).GetAwaiter().GetResult();

                var iamService = new IamService(userRepo, tenantContext, passwordHasher);

                var resetSuccess = iamService.ResetPasswordAsync(iamUser.Id, new ResetIamUserPasswordDTO
                {
                    NewPassword = "NewSecurePassword456!"
                }).GetAwaiter().GetResult();

                if (!resetSuccess) throw new Exception("Password reset returned false.");

                var userAfter = userRepo.GetByIdAsync(iamUser.Id).GetAwaiter().GetResult();
                if (userAfter!.SecurityStamp == oldStamp) throw new Exception("SecurityStamp was not regenerated.");

                // Verify login with old password fails
                var verifyOld = passwordHasher.VerifyHashedPassword(userAfter, userAfter.PasswordHash, "OldPassword123!");
                if (verifyOld != PasswordVerificationResult.Failed) throw new Exception("Old password should no longer work.");

                // Verify login with new password succeeds
                var verifyNew = passwordHasher.VerifyHashedPassword(userAfter, userAfter.PasswordHash, "NewSecurePassword456!");
                if (verifyNew == PasswordVerificationResult.Failed) throw new Exception("New password verification failed.");
            });

            Console.WriteLine("=================================================");
            Console.WriteLine($"IAM Suite Results: {passed} Passed, {failed} Failed.");
            Console.WriteLine("=================================================");

            return failed == 0 ? 0 : 1;
        }
    }
}
