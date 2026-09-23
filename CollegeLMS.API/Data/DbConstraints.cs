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

        // Lessons (текущее занятие — одно на курс)
        await db.Database.ExecuteSqlRawAsync(
            """
                CREATE UNIQUE INDEX IF NOT EXISTS ux_lessons_course_id_is_current
                ON lessons (course_id)
                WHERE is_current;
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

        // Notification settings
        await db.Database.ExecuteSqlRawAsync(
            """
                DO $$
                BEGIN
                    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'ck_notification_settings_time_range') THEN
                        ALTER TABLE notification_settings
                        ADD CONSTRAINT ck_notification_settings_time_range
                        CHECK (time >= INTERVAL '7 hours 30 minutes' AND time <= INTERVAL '8 hours 30 minutes');
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

        // Practice days (номера пар учебной практики — непустой массив значений 1..8)
        await db.Database.ExecuteSqlRawAsync(
            """
                DO $$
                BEGIN
                    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'ck_practice_days_pair_numbers_range') THEN
                        ALTER TABLE practice_days ADD CONSTRAINT ck_practice_days_pair_numbers_range
                        CHECK (array_length(pair_numbers, 1) >= 1 AND pair_numbers <@ ARRAY[1,2,3,4,5,6,7,8]);
                    END IF;
                END $$;
            """
        );

        // Чистка исторических дублей предметов: схлопываем повторные пробелы
        // и срезаем завершающие точки после цифры («МДК.01.03.» → «МДК.01.03»).
        // Блок идемпотентен: повторный запуск не меняет уже нормализованные значения.
        await db.Database.ExecuteSqlRawAsync(
            """
                UPDATE schedule_entries SET subject = btrim(regexp_replace(subject, '\s+', ' ', 'g')) WHERE subject IS NOT NULL AND subject <> btrim(regexp_replace(subject, '\s+', ' ', 'g'));
                UPDATE schedule_entries SET subject = regexp_replace(subject, '\.+$', '') WHERE subject IS NOT NULL AND subject ~ '\d\.+$';

                UPDATE schedule_history SET subject = btrim(regexp_replace(subject, '\s+', ' ', 'g')) WHERE subject IS NOT NULL AND subject <> btrim(regexp_replace(subject, '\s+', ' ', 'g'));
                UPDATE schedule_history SET subject = regexp_replace(subject, '\.+$', '') WHERE subject IS NOT NULL AND subject ~ '\d\.+$';

                UPDATE schedule_history SET removed_subject = btrim(regexp_replace(removed_subject, '\s+', ' ', 'g')) WHERE removed_subject IS NOT NULL AND removed_subject <> btrim(regexp_replace(removed_subject, '\s+', ' ', 'g'));
                UPDATE schedule_history SET removed_subject = regexp_replace(removed_subject, '\.+$', '') WHERE removed_subject IS NOT NULL AND removed_subject ~ '\d\.+$';

                UPDATE correction_positions SET subject = btrim(regexp_replace(subject, '\s+', ' ', 'g')) WHERE subject IS NOT NULL AND subject <> btrim(regexp_replace(subject, '\s+', ' ', 'g'));
                UPDATE correction_positions SET subject = regexp_replace(subject, '\.+$', '') WHERE subject IS NOT NULL AND subject ~ '\d\.+$';

                UPDATE correction_positions SET removed_subject = btrim(regexp_replace(removed_subject, '\s+', ' ', 'g')) WHERE removed_subject IS NOT NULL AND removed_subject <> btrim(regexp_replace(removed_subject, '\s+', ' ', 'g'));
                UPDATE correction_positions SET removed_subject = regexp_replace(removed_subject, '\.+$', '') WHERE removed_subject IS NOT NULL AND removed_subject ~ '\d\.+$';
            """
        );
    }
}
