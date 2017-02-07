import { DataphorFrontendClientPage } from './app.po';

describe('dataphor-frontend-client App', function() {
  let page: DataphorFrontendClientPage;

  beforeEach(() => {
    page = new DataphorFrontendClientPage();
  });

  it('should display message saying app works', () => {
    page.navigateTo();
    expect(page.getParagraphText()).toEqual('app works!');
  });
});
