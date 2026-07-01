using Microsoft.EntityFrameworkCore;

namespace CollegeLMS.API.Data;

public static class DbConstraints
{
    public static async Task EnsureAsync(AppDbContext db)
    {
        var sql = """

            -- Users
            DO $$
            BEGIN
                IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'ck_users_email_length') THEN
                    ALTER TABLE users ADD CONSTRAINT ck_users_email_length CHECK (length(email) > 0);
                END IF;
            END $$;

            """;

        await db.Database.ExecuteSqlRawAsync(sql);
    }
}
