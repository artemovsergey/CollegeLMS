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

        // Groups
        await db.Database.ExecuteSqlRawAsync(
            """
                DO $$
                BEGIN
                    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'ck_groups_course_range') THEN
                        ALTER TABLE groups ADD CONSTRAINT ck_groups_course_range CHECK (course BETWEEN 1 AND 4);
                    END IF;
                END $$;
            """
        );

        // Teachers
        await db.Database.ExecuteSqlRawAsync(
            """
                DO $$
                BEGIN
                    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'ck_teachers_department_not_empty') THEN
                        ALTER TABLE teachers ADD CONSTRAINT ck_teachers_department_not_empty CHECK (length(cyclical_commission) > 0);
                    END IF;
                    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'ck_teachers_position_not_empty') THEN
                        ALTER TABLE teachers ADD CONSTRAINT ck_teachers_position_not_empty CHECK (length(position) > 0);
                    END IF;
                END $$;
            """
        );

        // Students
        await db.Database.ExecuteSqlRawAsync(
            """
                DO $$
                BEGIN
                    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'ck_students_record_book_not_empty') THEN
                        ALTER TABLE students ADD CONSTRAINT ck_students_record_book_not_empty CHECK (length(record_book_number) > 0);
                    END IF;
                END $$;
            """
        );

        // Courses
        await db.Database.ExecuteSqlRawAsync(
            """
                DO $$
                BEGIN
                    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'ck_courses_title_not_empty') THEN
                        ALTER TABLE courses ADD CONSTRAINT ck_courses_title_not_empty CHECK (length(title) > 0);
                    END IF;
                END $$;
            """
        );

        // Assignments
        await db.Database.ExecuteSqlRawAsync(
            """
                DO $$
                BEGIN
                    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'ck_assignments_max_score_range') THEN
                        ALTER TABLE assignments ADD CONSTRAINT ck_assignments_max_score_range CHECK (max_score BETWEEN 1 AND 100);
                    END IF;
                END $$;
            """
        );

        // Schedule entries
        await db.Database.ExecuteSqlRawAsync(
            """
                DO $$
                BEGIN
                    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'ck_schedule_entries_time_range') THEN
                        ALTER TABLE schedule_entries ADD CONSTRAINT ck_schedule_entries_time_range CHECK (start_time < end_time);
                    END IF;
                END $$;
            """
        );

        // Tests
        await db.Database.ExecuteSqlRawAsync(
            """
                DO $$
                BEGIN
                    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'ck_tests_time_limit_positive') THEN
                        ALTER TABLE tests ADD CONSTRAINT ck_tests_time_limit_positive CHECK (time_limit_minutes > 0);
                    END IF;
                    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'ck_tests_passing_score_range') THEN
                        ALTER TABLE tests ADD CONSTRAINT ck_tests_passing_score_range CHECK (passing_score BETWEEN 0 AND 100);
                    END IF;
                    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'ck_tests_max_attempts_positive') THEN
                        ALTER TABLE tests ADD CONSTRAINT ck_tests_max_attempts_positive CHECK (max_attempts > 0);
                    END IF;
                END $$;
            """
        );

        // Semesters
        await db.Database.ExecuteSqlRawAsync(
            """
                DO $$
                BEGIN
                    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'ck_semesters_dates_range') THEN
                        ALTER TABLE semesters ADD CONSTRAINT ck_semesters_dates_range CHECK (end_date > start_date);
                    END IF;
                END $$;
            """
        );

        // Specialties
        await db.Database.ExecuteSqlRawAsync(
            """
                DO $$
                BEGIN
                    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'ck_specialties_code_not_empty') THEN
                        ALTER TABLE specialties ADD CONSTRAINT ck_specialties_code_not_empty CHECK (length(code) > 0);
                    END IF;
                END $$;
            """
        );

        // Test Assignments
        await db.Database.ExecuteSqlRawAsync(
            """
                DO $$
                BEGIN
                    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'ck_test_assignments_dates_range') THEN
                        ALTER TABLE test_assignments ADD CONSTRAINT ck_test_assignments_dates_range CHECK (close_date > open_date);
                    END IF;
                    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'ck_test_assignments_max_attempts_positive') THEN
                        ALTER TABLE test_assignments ADD CONSTRAINT ck_test_assignments_max_attempts_positive CHECK (max_attempts > 0);
                    END IF;
                END $$;
            """
        );

        // Submissions
        await db.Database.ExecuteSqlRawAsync(
            """
                DO $$
                BEGIN
                    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'ck_assignment_submissions_score_range') THEN
                        ALTER TABLE assignment_submissions ADD CONSTRAINT ck_assignment_submissions_score_range CHECK (score IS NULL OR (score >= 0 AND score <= 100));
                    END IF;
                END $$;
            """
        );
    }
}
