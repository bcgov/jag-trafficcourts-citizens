import { NgModule, APP_INITIALIZER, Injector } from '@angular/core';
import { Router } from '@angular/router';

import {
  KeycloakAngularModule,
  KeycloakService,
  KeycloakOptions,
} from 'keycloak-angular';

import { environment } from '@env/environment';
import { ToastService } from '@core/services/toast.service';
import { MockKeycloakService } from 'tests/mocks/mock-keycloak.service';
import { ConfigService } from '@config/config.service';

function initializer(
  keycloak: KeycloakService,
  injector: Injector
): () => Promise<void> {
  console.log('using Keycloak initializer');
  const routeToDefault = () => injector.get(Router).navigateByUrl('/');

  return async (): Promise<void> => {
    const authenticated = await keycloak.init(
      environment.keycloakConfig as KeycloakOptions
    );

    keycloak.getKeycloakInstance().onTokenExpired = () => {
      keycloak.updateToken().catch(() => {
        injector
          .get(ToastService)
          .openErrorToast(
            'Your session has expired, you will need to re-authenticate'
          );
        routeToDefault();
      });
    };

    if (authenticated) {
      // Ensure configuration is populated before the application
      // is fully initialized to prevent race conditions
      await injector.get(ConfigService).load().toPromise();

      // Force refresh to begin expiry timer.
      keycloak.updateToken(-1);
    }
  };
}

@NgModule({
  imports: [KeycloakAngularModule],
  providers: [
    {
      provide: KeycloakService,
      useClass: environment.useKeycloak ? KeycloakService : MockKeycloakService,
    },
    {
      provide: APP_INITIALIZER,
      useFactory: () => {
        return environment.useKeycloak
          ? initializer
          : MockKeycloakService.mockInitializer;
      },
      multi: true,
      deps: [KeycloakService, Injector],
    },
  ],
})
export class KeycloakModule {}
