using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DevFreela.Payments.API.Model
{
    public class PaymentApprovedIntegrationEvent
    {
        // precisa da informação do idProjeto para atualiza-lo
        public PaymentApprovedIntegrationEvent(int idProject)
        {
            IdProject = idProject;
        }

        public int IdProject { get; set; }
    }
}
