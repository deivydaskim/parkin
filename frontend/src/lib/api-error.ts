import { AxiosError } from 'axios'
import { z } from 'zod'

// RFC 7807 ProblemDetails — what the API returns via TypedResults.Problem / FastEndpoints.
const problemDetailsSchema = z.object({
  type: z.string().optional(),
  title: z.string().optional(),
  status: z.number().optional(),
  detail: z.string().optional(),
  errors: z.record(z.string(), z.array(z.string())).optional(),
})

export type ProblemDetails = z.infer<typeof problemDetailsSchema>

export interface ApiError {
  status?: number
  title?: string
  detail?: string
  /** User-facing message, always populated. */
  message: string
}

export function parseApiError(
  error: unknown,
  fallback = 'Something went wrong.',
): ApiError {
  if (error instanceof AxiosError) {
    if (!error.response) {
      return { message: 'Network error. Check your connection.' }
    }

    const parsed = problemDetailsSchema.safeParse(error.response.data)
    const pd = parsed.success ? parsed.data : undefined
    const firstValidation = pd?.errors
      ? Object.values(pd.errors)[0]?.[0]
      : undefined

    return {
      status: error.response.status,
      title: pd?.title,
      detail: pd?.detail,
      message: pd?.detail ?? firstValidation ?? pd?.title ?? fallback,
    }
  }

  return { message: fallback }
}
