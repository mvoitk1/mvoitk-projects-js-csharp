import { useParams } from "react-router-dom";

export function useCompanySlug(): string {
  const { companySlug } = useParams();

  if (!companySlug) {
    throw new Error("Missing companySlug in route.");
  }

  return companySlug;
}
