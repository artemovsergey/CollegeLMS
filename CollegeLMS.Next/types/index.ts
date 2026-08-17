export interface User {
  id: string
  login: string
  email: string
  fullName: string
  role: string
  teacherId?: string | null
  avatarUrl?: string | null
}

export interface LoginRequest {
  login: string
  password: string
}

export interface LoginResponse {
  token: string
  user: User
}

export interface CreateUserRequest {
  login: string
  email: string
  password: string
  fullName: string
  role: string
}

export interface UpdateUserRequest {
  login: string
  email: string
  fullName: string
  role: string
}

export interface ChangeRoleRequest {
  role: string
}

export interface Result<T> {
  isSuccess: boolean
  data: T | null
  errorMessage: string | null
  statusCode: number
  errors?: Record<string, string[]>
}

export interface GroupResponse {
  id: string
  name: string
  course: number
  studentCount: number
}

export interface CreateGroupRequest {
  name: string
  course: number
}

export interface TeacherResponse {
  id: string
  fullName: string
  email: string
  cyclicalCommission: string
  position: string
}

export interface StudentResponse {
  id: string
  fullName: string
  email: string
  groupId: string
  groupName: string
  recordBookNumber: string
}

export interface CourseResponse {
  id: string
  title: string
  description: string
  teacherId: string
  teacherName: string
  groupNames: string
  status: string
  isActive: boolean
  authorIds: string[]
  authorNames: string
  lectureCount: number
  assignmentCount: number
}

export interface CreateCourseRequest {
  title: string
  description: string
  authorIds: string[]
}

export interface LectureResponse {
  id: string
  courseId: string
  title: string
  content: string
  order: number
  lectureType: "Lecture" | "Practice" | "SelfStudy"
  testId: string | null
  testTitle: string | null
}

export interface AssignmentResponse {
  id: string
  courseId: string
  title: string
  description: string
  dueDate: string | null
  maxScore: number
  order: number
  submissionCount: number
}

export interface SubmissionResponse {
  id: string
  assignmentId: string
  studentId: string
  studentName: string
  filePath: string
  comment: string | null
  score: number | null
  submittedAt: string
}

export interface NewsCategoryResponse {
  id: string
  name: string
  slug: string
}

export interface NewsResponse {
  id: string
  title: string
  slug: string
  content: string
  imageUrl: string | null
  categoryId: string | null
  categoryName: string | null
  publishedAt: string
  createdAt: string
  createdByName: string
}

export interface CreateNewsRequest {
  title: string
  content: string
  imageUrl?: string
  categoryId?: string
}

export interface UpdateNewsRequest {
  title: string
  content: string
  imageUrl?: string
  categoryId?: string
}

export interface UploadResponse {
  url: string
}

export interface PagedResponse<T> {
  items: T[]
  totalCount: number
  page: number
  pageSize: number
  totalPages: number
}

export interface MaterialResponse {
  id: string
  courseId: string
  lectureId: string | null
  assignmentId: string | null
  fileName: string
  fileSize: number
  mimeType: string
  createdAt: string
}

export interface CourseWithProgressDto {
  id: string
  title: string
  teacherName: string
  completionPercent: number
  completedItems: number
  totalItems: number
}

export interface StudentDashboardResponse {
  courses: CourseWithProgressDto[]
}

export interface CourseBriefDto {
  id: string
  title: string
  groupNames: string
}

export interface TeacherDashboardResponse {
  courses: CourseBriefDto[]
}

export interface ImportResult {
  categoriesCreated: number
  postsImported: number
  postsSkipped: number
  errors: string[]
}

export interface FeedbackListItemDto {
  id: string
  name: string
  email: string
  message: string
  createdAt: string
}

export interface TestResponse {
  id: string
  title: string
  description: string
  courseId: string
  courseTitle: string
  maxAttempts: number
  timeLimitMinutes: number
  passingScore: number
  type: string
  autoCheck: boolean
  showCorrectAnswers: boolean
  shuffleQuestions: boolean
  shuffleOptions: boolean
  questionCount: number
}

export interface CreateTestRequest {
  title: string
  description: string
  courseId: string
  maxAttempts: number
  timeLimitMinutes: number
  passingScore: number
  type: string
  lectureId?: string | null
}

export interface UpdateTestRequest {
  title: string
  description: string
  maxAttempts: number
  timeLimitMinutes: number
  passingScore: number
  type: string
}

export interface TestQuestionResponse {
  id: string
  testId: string
  text: string
  type: string
  options: string
  correctAnswer: string
  points: number
  orderIndex: number
}

export interface CreateTestQuestionRequest {
  text: string
  type: string
  options: string
  correctAnswer: string
  points: number
  orderIndex: number
}

export interface UpdateTestQuestionRequest {
  text: string
  type: string
  options: string
  correctAnswer: string
  points: number
  orderIndex: number
}

export interface TestAssignmentResponse {
  id: string
  testId: string
  groupId: string
  groupName: string
  openDate: string
  closeDate: string
  maxAttempts: number
}

export interface CreateTestAssignmentRequest {
  groupId: string
  openDate: string
  closeDate: string
  maxAttempts: number
}

export interface TestAttemptResponse {
  id: string
  testId: string
  startedAt: string
  completedAt: string | null
  status: string
  score: number
  maxScore: number
}

export interface SpecialtyResponse {
  id: string
  code: string
  name: string
  description: string
  isActive: boolean
}

export interface CreateSpecialtyRequest {
  code: string
  name: string
  description: string
}

export interface UpdateSpecialtyRequest {
  code: string
  name: string
  description: string
  isActive: boolean
}

export interface ChangePasswordRequest {
  oldPassword: string
  newPassword: string
}

export interface TransferRecordResponse {
  id: string
  studentId: string
  fromGroupName: string
  toGroupName: string
  reason: string
  transferredAt: string
}

export interface CourseGroupResponse {
  groupId: string
  groupName: string
}

export interface CourseProgressResponse {
  courseId: string
  courseTitle: string
  totalAssignments: number
  completedAssignments: number
  totalTests: number
  completedTests: number
  averageScore: number
  completionPercent: number
}

export interface ImportProgressDto {
  importId: string
  status: "running" | "completed" | "failed" | "cancelled"
  total: number
  processed: number
  errors: number
  errorMessages: string[]
  result: ImportResult | null
}

export interface SearchResult {
  title: string
  type: "news" | "page"
  url: string
  snippet: string
  score: number
}

export interface TeacherProfileData {
  cyclicalCommission: string
  position: string
  category?: string
}

export interface StudentProfileData {
  groupId: string
  groupName: string
  recordBookNumber: string
}

export interface ProfileResponse {
  id: string
  login: string
  email: string
  fullName: string
  role: string
  avatarUrl?: string | null
  teacherData: TeacherProfileData | null
  studentData: StudentProfileData | null
}

export type TeacherCategory = "None" | "First" | "Higher"

export interface UpdateProfileRequest {
  fullName: string
  email: string
  cyclicalCommission?: string
  category?: string
}

export interface ChangePasswordRequest {
  oldPassword: string
  newPassword: string
}

export interface UserProfileResponse {
  user: User
  courses: { id: string; title: string }[]
}

export interface AdminDashboardResponse {
  userCount: number
  teacherCount: number
  studentCount: number
  courseCount: number
  groupCount: number
  newsCount: number
  feedbackCount: number
}

export interface ScheduleEntryResponse {
  id: string
  groupId: string
  groupName: string
  teacherId: string | null
  teacherName: string | null
  subject: string
  room: string
  dayOfWeek: number
  numberPair: number
  startTime: string
  endTime: string
  weeks: number[]
  lessonType: string
}

export interface CalendarDayResponse {
  day: string
  dayOfWeek: number
  entries: ScheduleEntryResponse[]
}

export interface CalendarResponse {
  weekStart: string
  days: CalendarDayResponse[]
}

export interface StartTestResponse {
  attemptId: string
  startedAt: string
  timeLimitMinutes: number
  questions: TestQuestionDto[]
}

export interface TestQuestionDto {
  id: string
  text: string
  type: string
  options: string
  orderIndex: number
}

export interface SubmitAnswersRequest {
  answers: AnswerDto[]
}

export interface AnswerDto {
  questionId: string
  givenAnswer: string
}

export interface TestResultResponse {
  attemptId: string
  score: number
  maxScore: number
  percentage: number
  passed: boolean
  completedAt: string
  answerReviews: AnswerReviewDto[]
}

export interface AnswerReviewDto {
  questionId: string
  questionText: string
  givenAnswer: string
  correctAnswer: string
  isCorrect: boolean
  points: number
}

export interface TestStatsResponse {
  totalAttempts: number
  passedCount: number
  failedCount: number
  averageScore: number
  medianScore: number
  maxScore: number
  minScore: number
  studentResults: StudentResultDto[]
}

export interface StudentResultDto {
  studentName: string
  groupName: string
  score: number
  maxScore: number
  passed: boolean
}

export interface MyTestResultDto {
  testId: string
  testTitle: string
  score: number
  maxScore: number
  percentage: number
  passed: boolean
  completedAt: string
}
