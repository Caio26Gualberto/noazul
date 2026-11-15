import { Injectable, computed, inject, signal } from '@angular/core';
import { TranslocoService } from '@jsverse/transloco';
import { Observable, map, tap } from 'rxjs';
import { ApiClient } from '../api/api-client';
import { CustomCategoryDto, PagedResultDto } from '../api/types';
import {
  CATEGORIES,
  Category,
  CustomCategory,
  CustomCategoryKey,
  isBuiltInCategory,
} from './finance.models';

const CUSTOM_CATEGORIES_PAGE_SIZE = 100;

@Injectable({ providedIn: 'root' })
export class CategoryStore {
  private readonly api = inject(ApiClient);
  private readonly transloco = inject(TranslocoService);

  private readonly customList = signal<CustomCategory[]>([]);

  readonly custom = this.customList.asReadonly();

  readonly all = computed<Category[]>(() => [
    ...CATEGORIES,
    ...this.customList().map((category) => category.key),
  ]);

  private readonly names = computed(
    () => new Map(this.customList().map((category) => [category.key, category.name])),
  );

  refresh(): void {
    this.api
      .get<PagedResultDto<CustomCategoryDto>>(
        `/api/custom-categories?pageSize=${CUSTOM_CATEGORIES_PAGE_SIZE}`,
        { silent: true },
      )
      .subscribe({
        next: (page) => this.customList.set(page.items.map(toCustomCategory)),
        error: () => undefined,
      });
  }

  label(category: Category): string {
    if (isBuiltInCategory(category)) {
      return this.transloco.translate(`finance.categories.${category}`);
    }
    return (
      this.names().get(category) ?? this.transloco.translate('finance.categories.unknownCustom')
    );
  }

  create(name: string): Observable<CustomCategory> {
    return this.api.post<CustomCategoryDto>('/api/custom-categories', { name }).pipe(
      map(toCustomCategory),
      tap((created) => this.customList.update((list) => sortByName([...list, created]))),
    );
  }

  rename(category: CustomCategory, name: string): Observable<CustomCategory> {
    return this.api
      .put<CustomCategoryDto>(`/api/custom-categories/${category.id}`, { name }, { announce: true })
      .pipe(
        map(toCustomCategory),
        tap((renamed) =>
          this.customList.update((list) =>
            sortByName(list.map((item) => (item.id === renamed.id ? renamed : item))),
          ),
        ),
      );
  }

  remove(category: CustomCategory): Observable<void> {
    return this.api
      .delete<void>(`/api/custom-categories/${category.id}`, { announce: true })
      .pipe(
        tap(() => this.customList.update((list) => list.filter((item) => item.id !== category.id))),
      );
  }
}

function toCustomCategory(dto: CustomCategoryDto): CustomCategory {
  return { id: dto.id, key: dto.key as CustomCategoryKey, name: dto.name };
}

function sortByName(list: CustomCategory[]): CustomCategory[] {
  return [...list].sort((a, b) => a.name.localeCompare(b.name, 'pt-BR'));
}
