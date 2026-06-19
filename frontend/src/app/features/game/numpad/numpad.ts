import { Component, Output, EventEmitter, HostListener } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-numpad',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './numpad.html',
  styleUrl: './numpad.css',
})
export class NumpadComponent {
  // Emits the validated score to the parent container component
  @Output() scoreSubmitted = new EventEmitter<number>();

  // Stores the current input locally before submission
  currentInput: string = '';

  // Intercepts physical keyboard inputs for seamless scoring operations
  @HostListener('window:keydown', ['$event'])
  handleKeyboardEvent(event: KeyboardEvent) {
    if (/^[0-9]$/.test(event.key)) {
      this.appendNumber(parseInt(event.key, 10));
    } else if (event.key === 'Enter') {
      this.submit();
    } else if (event.key === 'Backspace' || event.key === 'Delete') {
      this.clearInput();
    }
  }

  appendNumber(num: number) {
    // Prevents inputs longer than 3 characters and restricts the maximum score to 180
    if (this.currentInput.length < 3) {
      const newValue = Number(this.currentInput + num.toString());
      if (newValue <= 180) {
        this.currentInput += num.toString();
      }
    }
  }

  clearInput() {
    // Resets the current numpad input
    this.currentInput = '';
  }

  submit() {
    // Validates the input before emitting the event to the parent
    if (!this.currentInput) return;

    const score = parseInt(this.currentInput, 10);
    this.currentInput = '';

    // Transmits the final score to the smart container
    this.scoreSubmitted.emit(score);
  }
}
