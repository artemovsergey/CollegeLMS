SELECT
    g.name AS group_name,
    COALESCE(u.full_name, '') AS teacher_name,
    se.subject,
    se.room,
    se.day_of_week,
    se.number_pair,
    se.weeks,
    se.lesson_type
FROM schedule_entries se
JOIN groups g ON g.id = se.group_id
LEFT JOIN teachers t ON t.id = se.teacher_id
LEFT JOIN users u ON u.id = t.user_id
ORDER BY u.full_name, g.name, se.day_of_week, se.number_pair;
