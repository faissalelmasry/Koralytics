import { Component, Input, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-file-upload',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './file-upload.html',
  styleUrls: ['./file-upload.css']
})
export class FileUpload {
  @Input() accept: string = 'video/*,image/*'; 
  @Input() maxFileSizeMb: number = 50;
  @Input() isUploading: boolean = false;
  @Input() uploadProgress: number = 0;

  @Output() fileSelected = new EventEmitter<File>();

  selectedFile: File | null = null;
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
      this.validateAndProcessFile(event.dataTransfer.files[0]);
    }
  }

  onFileChange(event: Event) {
    const element = event.currentTarget as HTMLInputElement;
    if (element.files && element.files.length > 0) {
      this.validateAndProcessFile(element.files[0]);
    }
  }

  // التحقق من المساحة ونوع الملف
  private validateAndProcessFile(file: File) {
    this.errorMessage = '';
    const fileSizeMb = file.size / (1024 * 1024);

    if (fileSizeMb > this.maxFileSizeMb) {
      this.errorMessage = `file is too heavy. maximum allowed limit is ${this.maxFileSizeMb}mb.`;
      return;
    }

    this.selectedFile = file;
    this.fileSelected.emit(file);
  }

  clearFile(event: Event) {
    event.stopPropagation();
    this.selectedFile = null;
    this.errorMessage = '';
    this.uploadProgress = 0;
  }

  formatSize(bytes: number): string {
    if (bytes === 0) return '0 Bytes';
    const k = 1024;
    const dm = 2;
    const sizes = ['Bytes', 'KB', 'MB', 'GB'];
    const i = Math.floor(Math.log(bytes) / Math.log(k));
    return parseFloat((bytes / Math.pow(k, i)).toFixed(dm)) + ' ' + sizes[i];
  }
}