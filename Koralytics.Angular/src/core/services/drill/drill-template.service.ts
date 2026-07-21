import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  DrillTemplateDto,
  DrillCategoryDto,
  CreateDrillTemplateDto,
  UpdateDrillTemplateDto,
  TemplateFilterDto,
} from '../../interfaces/drill-template.model';

export interface PagedResultDto<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
  hasPreviousPage: boolean;
  hasNextPage: boolean;
}

@Injectable({
  providedIn: 'root'
})
export class DrillTemplateService {
  private readonly apiUrl = `${environment.apiUrl}/api/drills`;

  constructor(private http: HttpClient) { }
  private buildParams(filter: TemplateFilterDto): HttpParams {
    let params = new HttpParams()
      .set('pageNumber', filter.pageNumber.toString())
      .set('pageSize', filter.pageSize.toString());

    if (filter.searchTerm) {
      params = params.set('searchTerm', filter.searchTerm);
    }
    return params;
  }

  // ==========================================
  // CATEGORIES
  // ==========================================

  getDrillCategories(): Observable<DrillCategoryDto[]> {
    return this.http.get<DrillCategoryDto[]>(`${this.apiUrl}/categories`);
  }

  // ==========================================
  // TEMPLATES
  // ==========================================

  getTemplates(filter: TemplateFilterDto): Observable<PagedResultDto<DrillTemplateDto>> {
    const params = this.buildParams(filter);
    return this.http.get<PagedResultDto<DrillTemplateDto>>(`${this.apiUrl}/templates`, { params });
  }

  getTemplatesByCategory(categoryId: number, filter: TemplateFilterDto): Observable<PagedResultDto<DrillTemplateDto>> {
    const params = this.buildParams(filter);
    return this.http.get<PagedResultDto<DrillTemplateDto>>(`${this.apiUrl}/templates/category/${categoryId}`, { params });
  }

  getTemplateById(id: number): Observable<DrillTemplateDto> {
    return this.http.get<DrillTemplateDto>(`${this.apiUrl}/templates/${id}`);
  }

  createTemplate(dto: CreateDrillTemplateDto): Observable<DrillTemplateDto> {
    return this.http.post<DrillTemplateDto>(`${this.apiUrl}/templates`, dto);
  }

  updateTemplate(id: number, dto: UpdateDrillTemplateDto): Observable<DrillTemplateDto> {
    return this.http.put<DrillTemplateDto>(`${this.apiUrl}/templates/${id}`, dto);
  }

  shareTemplate(id: number): Observable<{ message: string }> {
    return this.http.patch<{ message: string }>(`${this.apiUrl}/templates/${id}/share`, {});
  }

  deleteTemplate(id: number): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/templates/${id}`);
  }
}