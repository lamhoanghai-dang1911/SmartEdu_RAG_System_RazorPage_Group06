document.addEventListener('DOMContentLoaded', function () {
    function logModalState(modal) {
        try {
            var backdrop = document.querySelector('.modal-backdrop');
            var styleModal = window.getComputedStyle(modal);
            var styleDialog = modal.querySelector('.modal-dialog') ? window.getComputedStyle(modal.querySelector('.modal-dialog')) : null;
            console.log('[modal-debug] modal id=' + modal.id + ' classes=' + modal.className);
            console.log('[modal-debug] modal pointer-events=' + styleModal.getPropertyValue('pointer-events') + ' zIndex=' + styleModal.zIndex + ' display=' + styleModal.display);
            if (styleDialog) console.log('[modal-debug] dialog zIndex=' + styleDialog.zIndex + ' pointer-events=' + styleDialog.getPropertyValue('pointer-events'));
            if (backdrop) console.log('[modal-debug] backdrop present zIndex=' + window.getComputedStyle(backdrop).zIndex + ' opacity=' + window.getComputedStyle(backdrop).opacity);
            else console.log('[modal-debug] backdrop not present');
        } catch (err) {
            console.error('[modal-debug] error', err);
        }
    }

    document.querySelectorAll('.modal').forEach(function (m) {
        m.addEventListener('show.bs.modal', function (e) {
            console.log('[modal-debug] show.bs.modal for', m.id);
            logModalState(m);
        });
        m.addEventListener('shown.bs.modal', function (e) {
            console.log('[modal-debug] shown.bs.modal for', m.id);
            logModalState(m);
        });
        m.addEventListener('hide.bs.modal', function (e) {
            console.log('[modal-debug] hide.bs.modal for', m.id);
        });
        m.addEventListener('hidden.bs.modal', function (e) {
            console.log('[modal-debug] hidden.bs.modal for', m.id);
            logModalState(m);
        });
    });

    // global click tracer to see where click events go when modal is open
    document.addEventListener('click', function (e) {
        console.log('[modal-debug] click target', e.target && (e.target.id || e.target.className || e.target.tagName));
    }, true);
});
