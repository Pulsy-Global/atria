/**
 * OSS error codes returned by the backend in ProblemDetails.errorCode.
 * Must stay in sync with backend constants.
 */
export const ErrorCodes = {
    CursorBehindTail: 'CURSOR_BEHIND_TAIL',
} as const;

export type ErrorCode = (typeof ErrorCodes)[keyof typeof ErrorCodes];
