import { Pipe, PipeTransform, inject } from '@angular/core';
import { CategoryStore } from './category.store';
import { Category } from './finance.models';

@Pipe({ name: 'categoryName', pure: false })
export class CategoryNamePipe implements PipeTransform {
  private readonly store = inject(CategoryStore);

  transform(category: Category): string {
    return this.store.label(category);
  }
}
