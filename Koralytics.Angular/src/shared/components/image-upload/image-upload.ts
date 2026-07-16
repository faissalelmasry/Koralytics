import { Component, Input, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-image-upload',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './image-upload.html',
  styleUrls: ['./image-upload.css']
})
export class ImageUpload {
  @Input() maxFileSizeMb: number = 5;
  @Input() isUploading: boolean = false;
  @Input() aspectRatio: 'square' | 'circle' = 'circle'; 
  @Output() imageSelected = new EventEmitter<File>();

  imagePreviewUrl: string | null = null;
  errorMessage: string = '';
  isDragOver: boolean = false;

  onDragOver(event: DragEvent) {
    event.preventDefault();
    event.stopPropagation();
    this.isDragOver = true;
  }

  onDragLeave(event: DragEvent) {
    event.preventDefault();
    event.stopPropagation();
    this.isDragOver = false;
  }

  onDrop(event: DragEvent) {
    event.preventDefault();
    event.stopPropagation();
    this.isDragOver = false;

    if (event.dataTransfer?.files && event.dataTransfer.files.length > 0) {
      this.validateAndProcessImage(event.dataTransfer.files[0]);
    }
  }

  onFileChange(event: Event) {
    const element = event.currentTarget as HTMLInputElement;
    if (element.files && element.files.length > 0) {
      this.validateAndProcessImage(element.files[0]);
    }
  }

  private validateAndProcessImage(file: File) {
    this.errorMessage = '';

    if (!file.type.startsWith('image/')) {
      this.errorMessage = 'invalid file type. please drop a valid tactical image.';
      return;
    }

    const fileSizeMb = file.size / (1024 * 1024);
    if (fileSizeMb > this.maxFileSizeMb) {
      this.errorMessage = `avatar image limit is ${this.maxFileSizeMb}mb maximum.`;
      return;
    }

    const reader = new FileReader();
    reader.onload = () => {
      this.imagePreviewUrl = reader.result as string;
    };
    reader.readAsDataURL(file);

    this.imageSelected.emit(file);
  }

  removeImage(event: Event) {
    event.stopPropagation();
    this.imagePreviewUrl = null;
    this.errorMessage = '';
  }
}