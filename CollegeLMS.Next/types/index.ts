export interface User {
  id: string
  login: string
  email: string
  fullName: string
  role: string
  isActive: boolean
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
  lectureCount: number
  assignmentCount: number
}

export interface CreateCourseRequest {
  title: string
  description: string
}

export interface LectureResponse {
  id: string
  courseId: string
  title: string
  content: string
  order: number
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
  isPublished: boolean
  publishedAt: string
  createdAt: string
  createdByName: string
}

export interface CreateNewsRequest {
  title: string
  content: string
  imageUrl?: string
  categoryId?: string
  isPublished?: boolean
  publishedAt?: string
}

export interface UpdateNewsRequest {
  title: string
  content: string
  imageUrl?: string
  categoryId?: string
  isPublished?: boolean
  publishedAt?: string
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

export interface ExamResponse {
  id: string
  subject: string
  groupId: string
  groupName: string
  examDate: string
  type: string
  teacherId: string
  teacherName: string
  semesterId: string
  semesterName: string
  status: string
}

export interface CreateExamRequest {
  subject: string
  groupId: string
  examDate: string
  type: string
  teacherId: string
  semesterId: string
}

export interface UpdateExamRequest {
  subject: string
  examDate: string
  type: string
  teacherId: string
  semesterId: string
  status: string
}

export interface RetakeResponse {
  id: string
  examId: string
  studentId: string
  studentName: string
  retakeDate: string
  status: string
}

export interface CreateRetakeRequest {
  studentId: string
  retakeDate: string
}

export interface UpdateRetakeStatusRequest {
  status: string
}

export interface TestResponse {
  id: string
  title: string
  description: string
  courseId: string
  courseName: string
  maxAttempts: number
  timeLimitMinutes: number
  passingScore: number
  type: string
}

export interface CreateTestRequest {
  title: string
  description: string
  courseId: string
  maxAttempts: number
  timeLimitMinutes: number
  passingScore: number
  type: string
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
  studentId: string
  studentName: string
  startedAt: string
  completedAt: string | null
  score: number | null
  status: string
}

export interface SemesterResponse {
  id: string
  name: string
  startDate: string
  endDate: string
  type: string
}

export interface CreateSemesterRequest {
  name: string
  startDate: string
  endDate: string
  type: string
}

export interface UpdateSemesterRequest {
  name: string
  startDate: string
  endDate: string
  type: string
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

export interface StipendListResponse {
  id: string
  title: string
  semesterId: string
  semesterName: string
  studentCount: number
  createdAt: string
}

export interface StipendListItemResponse {
  id: string
  studentId: string
  studentName: string
  groupName: string
  amount: number
  type: string
  comment: string | null
}

export interface GenerateStipendRequest {
  semesterId: string
  type: string
  minScore: number
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
  id: string
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
  isActive: boolean
  teacherData: TeacherProfileData | null
  studentData: StudentProfileData | null
}

export interface UpdateProfileRequest {
  fullName: string
  email: string
}

export interface ChangePasswordRequest {
  oldPassword: string
  newPassword: string
}
