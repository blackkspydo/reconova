export interface Toast {
	id: number;
	message: string;
	variant: 'success' | 'error' | 'info' | 'warning';
}

let toasts = $state<Toast[]>([]);
let nextId = 0;

export function getToastStore() {
	return {
		get toasts() { return toasts; },

		add(message: string, variant: Toast['variant'] = 'info', duration = 4000) {
			const id = nextId++;
			toasts = [...toasts, { id, message, variant }];
			setTimeout(() => {
				toasts = toasts.filter(t => t.id !== id);
			}, duration);
		},

		remove(id: number) {
			toasts = toasts.filter(t => t.id !== id);
		},

		success(message: string) { this.add(message, 'success'); },
		error(message: string) { this.add(message, 'error', 6000); },
		info(message: string) { this.add(message, 'info'); },
		warning(message: string) { this.add(message, 'warning'); },
	};
}
