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
  department: string
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
  groupId: string
  groupName: string
  status: string
  lectureCount: number
  assignmentCount: number
}

export interface CreateCourseRequest {
  title: string
  description: string
  groupId: string
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

export interface TeacherDashboardResponse {
  coursesCount: number
  studentsCount: number
  recentSubmissions: SubmissionResponse[]
  courses: { id: string; title: string }[]
}

export interface StudentDashboardResponse {
  coursesCount: number
  recentGrades: { courseName: string; score: number | null }[]
  upcomingDeadlines: { assignmentTitle: string; dueDate: string | null }[]
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

export interface ImportProgressDto {
  importId: string
  status: "running" | "completed" | "failed"
  total: number
  processed: number
  errors: number
  errorMessages: string[]
  result: ImportResult | null
}
