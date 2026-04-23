import { MatDialog } from '@angular/material/dialog';
import { Observable } from 'rxjs';
import { ErrorCodes } from '../../core/constants/error-codes';
import { getErrorCode } from '../../core/errors/get-error-code';
import { ConfirmModalComponent, ConfirmModalData } from '../confirm/confirm-modal.component';

export function isCursorBehindTailError(error: any): boolean {
    return getErrorCode(error) === ErrorCodes.CursorBehindTail;
}

export function openCursorResetConfirm(
    dialog: MatDialog,
    feedName: string
): Observable<boolean> {
    const data: ConfirmModalData = {
        title: 'Cursor Behind Chain Tail',
        message:
            `Required blocks for "${feedName}" are no longer in the realtime source. ` +
            `Reset will restart the feed from the current chain head.`,
        confirmText: 'Reset Cursor',
        cancelText: 'Cancel',
        type: 'danger'
    };

    return dialog.open(ConfirmModalComponent, {
        width: '480px',
        data,
        autoFocus: false,
        restoreFocus: false
    }).afterClosed();
}
